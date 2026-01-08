using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SLA_API_AIChatBot_Poc.Configuration;
using SLA_API_AIChatBot_Poc.Interface;
using SLA_API_AIChatBot_Poc.Model;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace SLA_API_AIChatBot_Poc.Services
{
    public class OllamaService : IOllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly OllamaSettings _settings;
        private readonly ILogger<OllamaService> _logger;

        public OllamaService(
            IHttpClientFactory httpClientFactory,
            IOptions<OllamaSettings> settings,
            ILogger<OllamaService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("Ollama");
            _settings = settings.Value;
            _logger = logger;
        }

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };


        public async Task<string> GenerateChatResponseAsync(string prompt, List<Message>? conversationHistory = null,
            CancellationToken cancellationToken = default
)
        {
            try
            {
                var messages = BuildMessageList(prompt, conversationHistory);

                var request = new
                {
                    model = _settings.ChatModel,
                    messages = prompt,
                    stream = _settings.StreamingEnabled,
                    options = new
                    {
                        temperature = _settings.Temperature,
                        num_predict = _settings.MaxTokens,
                        num_ctx = _settings.ContextWindow
                    }
                };
                _logger.LogInformation("Sending chat request to Ollama with model: {Model}", _settings.ChatModel);

                var response = await _httpClient.PostAsJsonAsync("/api/chat", request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>();

                if (result?.Message?.Content == null)
                {
                    throw new Exception("Invalid response from Ollama service");
                }
                return result.Message.Content;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while calling Ollama service");
                throw new Exception("Failed to connect to Ollama service. Please ensure Ollama is running on http://localhost:11434", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating chat response");
                throw;
            }
        }


        public async IAsyncEnumerable<string> GenerateChatResponseStreamAsync(
               string prompt,
               List<Message>? conversationHistory = null,
               [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var messages = BuildMessageList(prompt, conversationHistory);

            var request = new
            {
                model = _settings.ChatModel,
                messages = messages,
                stream = true,
                options = new
                {
                    temperature = _settings.Temperature,
                    num_predict = _settings.MaxTokens,
                    num_ctx = _settings.ContextWindow
                }
            };

            _logger.LogInformation("Sending streaming chat request to Ollama");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating streaming request");
                throw new Exception("Failed to connect to Ollama service", ex);
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                OllamaChatResponse? chatResponse = null;
                try
                {
                    chatResponse = JsonSerializer.Deserialize<OllamaChatResponse>(line);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse streaming response line: {Line}", line);
                    continue;
                }

                if (chatResponse?.Message?.Content != null)
                {
                    yield return chatResponse.Message.Content;
                }

                if (chatResponse?.Done == true)
                {
                    _logger.LogInformation("Streaming completed");
                    break;
                }
            }
        }

      /*  public IAsyncEnumerable<string> GenerateChatResponseStreamAsync(string prompt, List<Message>? conversationHistory = null)
        {
            throw new NotImplementedException();
        }*/

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            try
            {
                var request = new
                {
                    model = _settings.EmbeddingModel,
                    prompt = text
                };

                _logger.LogDebug("Generating embedding for text of length: {Length}", text.Length);

                var response = await _httpClient.PostAsJsonAsync("/api/embeddings", request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();

                if (result?.Embedding == null || result.Embedding.Length == 0)
                {
                    throw new Exception("Invalid embedding response from Ollama");
                }

                return result.Embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding");
                throw;
            }
        }
        public async Task<List<float[]>> GenerateEmbeddingsBatchAsync(List<string> texts)
        {
            var embeddings = new List<float[]>();

            // Process in parallel with a limit to avoid overwhelming Ollama
            var semaphore = new SemaphoreSlim(3); // Max 3 concurrent requests
            var tasks = texts.Select(async text =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await GenerateEmbeddingAsync(text);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            embeddings = (await Task.WhenAll(tasks)).ToList();

            _logger.LogInformation("Generated {Count} embeddings in batch", embeddings.Count);

            return embeddings;
        }
        public async Task<string> GenerateRAGResponseAsync(
           string query,
           List<string> retrievedContexts,
           List<Message>? conversationHistory = null)
        {
            var ragPrompt = BuildRAGPrompt(query, retrievedContexts);
            return await GenerateChatResponseAsync(ragPrompt, conversationHistory);
        }
        public async IAsyncEnumerable<string> GenerateRAGResponseStreamAsync(
            string query,
            List<string> retrievedContexts,
            List<Message>? conversationHistory = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var ragPrompt = BuildRAGPrompt(query, retrievedContexts);

            await foreach (var chunk in GenerateChatResponseStreamAsync(ragPrompt, conversationHistory, cancellationToken))
            {
                yield return chunk;
            }
        }


        public async Task<bool> IsServiceAvailableAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ollama service is not available");
                return false;
            }
        }
        public async Task<List<string>> ListModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/tags");
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<OllamaModelsResponse>();

                return result?.Models?.Select(m => m.Name).ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing models");
                return new List<string>();
            }
        }
        #region Private Helper Methods

        private static List<OllamaMessage> BuildMessageList(string prompt, List<Message>? conversationHistory)
        {
            var messages = new List<OllamaMessage>();


            if (conversationHistory != null && conversationHistory.Any())
            {


                foreach (var m in conversationHistory)
                {
                    if (string.IsNullOrWhiteSpace(m?.Content)) continue;

                    messages.Add(new OllamaMessage
                    {
                        Role = NormalizeRole(m.Role),
                        Content = m.Content
                    });
                }

            }

            if (!string.IsNullOrWhiteSpace(prompt))
            {
                messages.Add(new OllamaMessage
                {
                    Role = "user",
                    Content = prompt
                });
            }


            return messages;
        }

        // <summary>
        /// Normalizes arbitrary role strings to Ollama-compatible roles.
        /// </summary>
        private static string NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return "user";

            var r = role.Trim().ToLowerInvariant();
            return r switch
            {
                "assistant" or "ai" or "bot" => "assistant",
                "system" or "sys" => "system",
                _ => "user"
            };
        }





        private string BuildRAGPrompt(string query, List<string> retrievedContexts)
        {
            var contextText = string.Join("\n\n---\n\n", retrievedContexts.Select((ctx, idx) =>
                $"Context {idx + 1}:\n{ctx}"));

            return $@"You are a helpful assistant. Answer the user's question based on the provided context.

            Context Information:
            {contextText}

            User Question: {query}

            Instructions:
            - Answer the question based ONLY on the information provided in the context above
            - If the context doesn't contain enough information to answer the question, clearly state that
            - Be specific and cite which context you're referencing when relevant
            - Provide a clear and concise answer

            Answer:";
        }

        public async IAsyncEnumerable<string> GenerateRAGResponseStreamAsync(string query, List<string> retrievedContexts, List<Message>? conversationHistory = null)
        {

            // If no contexts available, fall back to normal streaming
            if (retrievedContexts == null || retrievedContexts.Count == 0)
            {
                await foreach (var chunk in GenerateChatResponseStreamAsync(query, conversationHistory))
                {
                    yield return chunk;
                }
                yield break;
            }

            // Build RAG-aware message list
            var messages = BuildRAGMessages(query, retrievedContexts, conversationHistory);

            var request = new
            {
                model = _settings.ChatModel,
                messages = messages,    // Full history + system RAG context + user query
                stream = true,
                options = new

                {
                    temperature = _settings.Temperature,
                    num_predict = _settings.MaxTokens,
                    num_ctx = _settings.ContextWindow
                }
            };

            _logger.LogInformation("Sending RAG streaming chat request to Ollama (model: {Model}) with {ContextCount} contexts",
                    _settings.ChatModel, retrievedContexts.Count);

            HttpResponseMessage response;
            try
            {

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
                {
                    Content = JsonContent.Create(request, options: _jsonOptions)
                };
                 // Optional but sometimes useful if your upstream uses SSE:
                requestMessage.Headers.Accept.ParseAdd("text/event-stream");
                // Or for NDJSON: requestMessage.Headers.Accept.ParseAdd("application/x-ndjson");

                // Cancellation token is recommended for streaming scenarios
                response = await _httpClient.SendAsync(
                    requestMessage,
                    HttpCompletionOption.ResponseHeadersRead
                    
                );


                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating RAG streaming request");
                throw new Exception("Failed to connect to Ollama service", ex);
            }
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Handle SSE prefix if present: "data: {json}"
                if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    line = line.Substring("data:".Length).Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                }

                OllamaChatResponse? chatResponse = null;
                try
                {
                    chatResponse = JsonSerializer.Deserialize<OllamaChatResponse>(line, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse RAG streaming response line: {Line}", line);
                    continue;
                }

                if (!string.IsNullOrEmpty(chatResponse?.Message?.Content))
                {
                    yield return chatResponse!.Message!.Content!;
                }
                if (chatResponse?.Done == true)
                {
                    _logger.LogInformation("RAG streaming completed");
                    break;
                }
            }


        }

        /// <summary>
        /// Builds an Ollama-compatible message list with a System message that embeds RAG contexts,
        /// optional conversation history, and the user's query as the latest message.
        /// </summary>
        private static List<OllamaMessage> BuildRAGMessages(
            string query,
            List<string> contexts,
            List<Message>? history)
        {
            var messages = new List<OllamaMessage>();

            // 1) System prompt with the retrieved contexts
            var systemContent = BuildRagSystemContent(contexts);
            messages.Add(new OllamaMessage
            {
                Role = "system",
                Content = systemContent
            });

            // 2) Prior conversation history (mapped to Ollama's schema)
            if (history is not null && history.Count > 0)
            {
                foreach (var m in history)
                {
                    if (string.IsNullOrWhiteSpace(m?.Content)) continue;

                    messages.Add(new OllamaMessage
                    {
                        Role = NormalizeRole(m.Role),
                        Content = m.Content
                    });
                }
            }
            // 3) Current user query
            messages.Add(new OllamaMessage
            {
                Role = "user",
                Content = query
            });

            return messages;
        }

        /// <summary>
        /// Formats retrieved contexts into a single system message that instructs the model
        /// how to use them safely (RAG behavior).
        /// </summary>
        private static string BuildRagSystemContent(List<string> contexts)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are a helpful assistant. Use ONLY the provided context to answer the user's question.");
            sb.AppendLine("If the answer is not present in the context, say you don't know rather than guessing.");
            sb.AppendLine();
            sb.AppendLine("=== Retrieved Context ===");

            int index = 1;
            foreach (var ctx in contexts.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                sb.AppendLine($"[#{index}]");
                sb.AppendLine(ctx.Trim());
                sb.AppendLine("---");
                index++;
            }

            // Optional: add guidance to quote or refer to context indices if needed.
            // sb.AppendLine("When relevant, reference context indices like [#1], [#2].");

            return sb.ToString();
        }







        #endregion

        #region Response Models
        public class OllamaChatResponse
        {
            public OllamaMessage? Message { get; set; }
            public bool Done { get; set; }
            public string? Error { get; set; }
        }

        public class OllamaEmbeddingResponse
        {
            public float[] Embedding { get; set; } = Array.Empty<float>();
        }

        private class OllamaModelsResponse
        {
            public List<ModelInfo>? Models { get; set; }
        }

        private class ModelInfo
        {
            public string Name { get; set; } = string.Empty;
            public string ModifiedAt { get; set; } = string.Empty;
            public long Size { get; set; }
        }

        #endregion
    }
}

