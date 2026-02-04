using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using POC_SLAIS_Chat.Model;

using SLA_API_AIChatBot_Poc.Model;
using SLA_API_AIChatBot_Poc.Services;
using System.Text;
using System.Text.Json;

namespace SLA_API_AIChatBot_Poc.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public ChatController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }


        [HttpPost("ask")]
        public async Task<IActionResult> AskModel([FromBody] ChatRequest request)
        {

            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new ErrorResponse { Error = "Message is required." });

            try
            {
                var ollamaRequest = new
                {
                    model = "llama3.2", // or your model name
                    prompt = request.Message,
                    stream = false // Non-streaming
                };

                var response = await _httpClient.PostAsJsonAsync(
                    "http://localhost:11434/api/generate",
                    ollamaRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode,
                        new ErrorResponse { Error = $"Ollama Error: {error}" });
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                string modelResponse = doc.RootElement.GetProperty("response").GetString();

                var chatResponse = new ChatResponse
                {
                    Reply = modelResponse,
                    ConversationId = request.ConversationId,
                    RequiresEscalation = false,
                    Intent = null
                };

                return Ok(chatResponse);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(503, new ErrorResponse
                {
                    Error = "Unable to connect to Ollama service."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "An error occurred while processing your message."
                });
            }
        }


        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }


            string extractedText = System.IO.File.ReadAllText(filePath);

            DocumentCache.AddOrUpdate(file.FileName, extractedText);

            return Ok(new { message = "File uploaded successfully", fileName = file.FileName });

        }

        [HttpPost("askWithDoc")]
        public async Task<IActionResult> AskWithDoc([FromBody] DocChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new ErrorResponse { Error = "Question is required." });

            var docText = DocumentCache.Get(request.DocumentId);
            if (string.IsNullOrEmpty(docText))
                return BadRequest(new ErrorResponse { Error = "Document not found in cache." });

            try
            {
                var ollamaRequest = new
                {
                    model = "llama3.2",
                    prompt = $@"
                            You are a chatbot that must only answer using the cached document context.
                {docText}

                    Question: {request.Question}
                    Answer:",
                    stream = false
                };

                var response = await _httpClient.PostAsJsonAsync("http://localhost:11434/api/generate", ollamaRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode,
                        new ErrorResponse { Error = $"Ollama Error: {error}" });
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                string modelResponse = doc.RootElement.GetProperty("response").GetString();

                return Ok(new { reply = modelResponse });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Error = $"Error: {ex.Message}" });
            }
        }
        [HttpPost("askFromKB")]
        public async Task<IActionResult> AskFromKnowledgeBase([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new ErrorResponse
                {
                    Error = "Message is required."
                });

            var kbEntry = KnowledgeBase.GetAnswer(request.Message);

            if (kbEntry != null)
            {
                return Ok(new
                {
                    reply = kbEntry.Answer,
                    source = kbEntry.Source,
                    conversationId = request.ConversationId
                });
            } // Fallback to Ollama

            var ollamaRequest = new
            {
                model = "llama3.2",
                prompt = request.Message,
                stream = false
            };

            var response = await _httpClient.PostAsJsonAsync("http://localhost:11434/api/generate", ollamaRequest);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            string modelResponse = doc.RootElement.GetProperty("response").GetString();
            // Add new Q&A with source "Ollama"
            KnowledgeBase.AddNewQA(request.Message, modelResponse, "Ollama");
            return Ok(new
            {
                reply = modelResponse,
                source = "Ollama",
                conversationId = request.ConversationId
            });
        }

       
    }



}


