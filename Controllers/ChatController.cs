using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POC_SLAIS_Chat.Model;
using SLA_API_AIChatBot_Poc.Interface;
using SLA_API_AIChatBot_Poc.Model;
using System.Text.Json;

namespace SLA_API_AIChatBot_Poc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IOllamaService _ollamaService;
        private readonly ILogger<ChatController> _logger;
        private readonly AppDbContext _dbContext;

        public ChatController(
                IOllamaService ollamaService,
                ILogger<ChatController> logger,
                AppDbContext dbContext)
        {
            _ollamaService = ollamaService;
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpPost("stream")]
        public async Task StreamChat([FromBody] ChatRequest request)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");
            HttpContext.Response.Headers.Add("X-Accel-Buffering", "no");
            try
            {

                // Load or create conversation
                var conversation = await _dbContext.ConversationsContext.FirstOrDefaultAsync(c => c.ConversationId == request.ConversationId);

                if (conversation == null)
                {
                    conversation = new Conversations
                    {
                        ConversationId = request.ConversationId ?? Guid.NewGuid().ToString(),
                        StartedAt = DateTime.UtcNow
                    };
                    _dbContext.ConversationsContext.Add(conversation);
                }


                // Add user message
                conversation.AddMessage("user", request.Message);


                await foreach (var chunk in _ollamaService.GenerateChatResponseStreamAsync(
                    request.Message,
                    conversation.Messages,
                    HttpContext.RequestAborted))
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        content = chunk,
                        done = false,
                        conversationId = request.ConversationId
                    });

                    await Response.WriteAsync($"data: {json}\n\n");
                    await Response.Body.FlushAsync();
                }

                // Send completion message
                var doneJson = JsonSerializer.Serialize(new
                {
                    content = "",
                    done = true,
                    conversationId = request.ConversationId
                });

                await Response.WriteAsync($"data: {doneJson}\n\n");
                await Response.Body.FlushAsync();


                // Add bot response (full text)
                conversation.AddMessage("assistant", "[streamed response completed]");
                await _dbContext.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in streaming chat");

                var errorJson = JsonSerializer.Serialize(new
                {
                    content = "An error occurred. Please try again.",
                    done = true,
                    error = true
                });

                await Response.WriteAsync($"data: {errorJson}\n\n");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {

                var conversation = await _dbContext.ConversationsContext.FirstOrDefaultAsync(c => c.ConversationId == request.ConversationId);

                if (conversation == null)
                {
                    conversation = new Conversations
                    {
                        ConversationId = request.ConversationId ?? Guid.NewGuid().ToString(),
                        StartedAt = DateTime.UtcNow
                    };
                    _dbContext.ConversationsContext.Add(conversation);
                }

                // Add user message
                conversation.AddMessage("user", request.Message);

                // Generate response
                var response = await _ollamaService.GenerateChatResponseAsync(
                    request.Message,
                    conversation.Messages);


                // Add bot message
                conversation.AddMessage("assistant", response);

                await _dbContext.SaveChangesAsync();


                return Ok(new ChatResponse
                {
                    Reply = response,
                    ConversationId = request.ConversationId,
                    RequiresEscalation = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat request");
                return StatusCode(500, new { error = "An error occurred processing your request" });
            }
        }


        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            var isAvailable = await _ollamaService.IsServiceAvailableAsync();

            if (isAvailable)
            {
                var models = await _ollamaService.ListModelsAsync();
                return Ok(new
                {
                    status = "healthy",
                    models = models,
                    timestamp = DateTime.UtcNow
                });
            }

            return StatusCode(503, new
            {
                status = "unhealthy",
                error = "Ollama service is not available"
            });
        }
    }
}
   

