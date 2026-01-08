
using SLA_API_AIChatBot_Poc.Interface;
using SLA_API_AIChatBot_Poc.Model;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SLA_API_AIChatBot_Poc.Services
{
    public class ChatService 
    {

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<ChatService> _logger;
        private readonly IConversationRepository _conversationRepo;
      


        // In-memory cache for active conversations (use Redis in production)
        private readonly Dictionary<string, Conversations> _activeConversations;


        public ChatService(HttpClient httpClient, IConfiguration config, ILogger<ChatService> logger, IConversationRepository conversationRepo)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            _conversationRepo = conversationRepo;
         
            _activeConversations = new Dictionary<string, Conversations>();
        }
/*
        public async Task<ChatServiceResponse> GetResponseAsync(string userMessage, string? conversationId, string? userId = null)
        {
            conversationId ??= Guid.NewGuid().ToString();

            // Get or create conversation context
            var context = await GetOrCreateContextAsync(conversationId, userId);

            // Add user message to history
            context.AddMessage("user", userMessage);
            try
            {
                // Step 1: Analyze user intent
                var intent = AnalyzeIntent(userMessage);
                _logger.LogInformation("Detected intent: {Intent} for conversation {ConversationId}",
                    intent, conversationId);


                // Step 2: Try quick response for common queries
                //  var quickResponse = await TryQuickResponseAsync(userMessage, intent);
                *//*       if (quickResponse != null)
                       {
                           context.AddMessage("assistant", quickResponse);
                           await _conversationRepo.SaveConversationAsync(context);

                           return new ChatServiceResponse
                           {
                               Message = quickResponse,
                               ConversationId = conversationId,
                               RequiresEscalation = false,
                               Intent = intent

                           };
                       }*//*


                // Step 4: Check if escalation is needed
                if (ShouldEscalate(userMessage, intent, context))
                {
                    var escalationMessage = "I understand this is important. Let me connect you with a human agent who can better assist you. Please hold for a moment.";
                    context.AddMessage("assistant", escalationMessage);
                    context.RequiresEscalation = true;

                    await _conversationRepo.SaveConversationAsync(context);
                    await NotifyHumanAgentsAsync(conversationId, context);

                    return new ChatServiceResponse
                    {
                        Message = escalationMessage,
                        ConversationId = conversationId,
                        RequiresEscalation = true,
                        Intent = intent
                    };
                }
            }
            catch (Exception ex) {
                 Console.WriteLine(ex.ToString());        
            }
        }*/
  
        
        public async Task<bool> EscalateToHumanAsync(string conversationId)
        {
            var context = await _conversationRepo.GetConversationAsync(conversationId);
            if (context == null) return false;

            context.RequiresEscalation = true;
            await _conversationRepo.SaveConversationAsync(context);
            await NotifyHumanAgentsAsync(conversationId, context);

            return true;
        }

        private async Task NotifyHumanAgentsAsync(string conversationId, Conversations context)
        {
            // Send notification to agent queue/dashboard
            _logger.LogInformation("Escalating conversation {ConversationId} to human agents", conversationId);

            // In production: Send to SignalR hub, queue system, or agent dashboard
            await Task.CompletedTask;
        }

        private double CalculateConfidence(string response)
        {
            // Simple heuristic - in production, use more sophisticated methods
            if (response.Contains("I apologize") || response.Contains("I'm not sure"))
                return 0.5;

            if (response.Contains("let me connect you") || response.Contains("human agent"))
                return 0.3;

            return 0.85;
        }


        private async Task<string> GenerateAIResponseAsync(Conversations context, string intent, string? knowledgeContext)
        {
            var systemPrompt = BuildSystemPrompt(intent, knowledgeContext);

            // Build messages for AI
            var messages = new List<object> { new { role = "system", content = systemPrompt } };

            // Add conversation history (last 10 messages to manage token count)
            messages.AddRange(context.Messages
                .TakeLast(10)
                .Select(m => new { role = m.Role, content = m.Content }));

            var useOllama = _config.GetValue<bool>("AI:UseOllama", false);



            return await CallOllamaAsync(messages);


        }

        private async Task<Conversations> GetOrCreateContextAsync(string conversationId, string? userId)
        {
            // Try to get from cache first
            if (_activeConversations.TryGetValue(conversationId, out var cached))
            {
                return cached;
            }

            // Try to load from database
            var context = await _conversationRepo.GetConversationAsync(conversationId);

            if (context == null)
            {
                // Create new conversation
                context = new Conversations
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    StartedAt = DateTime.UtcNow,
                    Messages = new List<Message>()
                };

                // Add welcome message
                context.AddMessage("assistant",
                    "Hello! I'm here to help you with any questions or issues. How can I assist you today?");
            }

            _activeConversations[conversationId] = context;
            return context;
        }

        private string AnalyzeIntent(string message)
        {
            var lower = message.ToLower();

            // Order tracking
            if (Regex.IsMatch(lower, @"\b(track|where|status).*\border\b"))
                return "order_tracking";

            // Returns/Refunds
            if (Regex.IsMatch(lower, @"\b(return|refund|money back|cancel order)\b"))
                return "return_refund";

            // Account issues
            if (Regex.IsMatch(lower, @"\b(password|login|account|sign in|locked out)\b"))
                return "account_issue";

            // Billing
            if (Regex.IsMatch(lower, @"\b(charge|bill|payment|credit card|invoice)\b"))
                return "billing";

            // Product inquiry
            if (Regex.IsMatch(lower, @"\b(product|item|stock|available|price)\b"))
                return "product_inquiry";

            // Complaint
            if (Regex.IsMatch(lower, @"\b(complaint|angry|upset|terrible|awful|disappointed)\b"))
                return "complaint";

            // Technical support
            if (Regex.IsMatch(lower, @"\b(error|bug|not working|broken|fix)\b"))
                return "technical_support";

            return "general_inquiry";
        }
        /*  private async Task<string?> TryQuickResponseAsync(string message, string intent)
          {
              switch (intent)
              {
                  case "order_tracking":

                      if (true)
                      {


                          return await w;

                      }
                      return null; // Let AI handle if we can't find order

                  case "account_issue":
                      if (message.ToLower().Contains("password"))
                      {
                          return "I can help you reset your password. I've sent a password reset link to your registered email address. " +
                                 "Please check your inbox (and spam folder) and follow the instructions. The link will expire in 1 hour.";
                      }
                      break;

                  case "return_refund":
                      return "I can help you with a return. Our return policy allows returns within 30 days of purchase for a full refund. " +
                             "Could you provide your order number so I can process this for you?";
              }

              return null;
          }*/

        private bool ShouldEscalate(string message, string intent, Conversations context)
        {
            // Escalate if customer is frustrated (multiple similar messages)
            if (context.Messages.Count(m => m.Role == "user") > 5 &&
                context.Messages.TakeLast(3).All(m => m.Content.Length < 50))
            {
                return true;
            }

            // Escalate for complaints
            if (intent == "complaint")
            {
                return true;
            }

            // Escalate if explicitly requested
            if (Regex.IsMatch(message.ToLower(), @"\b(human|agent|person|representative|speak to someone)\b"))
            {
                return true;
            }

            // Escalate for complex legal/financial issues
            if (Regex.IsMatch(message.ToLower(), @"\b(lawyer|legal|sue|lawsuit|dispute)\b"))
            {
                return true;
            }

            return false;
        }

        private string BuildSystemPrompt(string intent, string? knowledgeContext)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are a helpful, professional customer service assistant.");
            sb.AppendLine("Be concise, friendly, and solution-focused.");
            sb.AppendLine("If you don't know something, admit it and offer to escalate.");
            sb.AppendLine();

            sb.AppendLine($"Current intent: {intent}");



            if (!string.IsNullOrEmpty(knowledgeContext))
            {
                sb.AppendLine();
                sb.AppendLine("Relevant company information:");
                sb.AppendLine(knowledgeContext);
            }

            sb.AppendLine();
            sb.AppendLine("Guidelines:");
            sb.AppendLine("- Keep responses under 100 words unless detailed explanation needed");
            sb.AppendLine("- Ask clarifying questions if needed");
            sb.AppendLine("- Always be empathetic and understanding");
            sb.AppendLine("- Provide specific next steps when possible");

            return sb.ToString();
        }

        private async Task<string> CallOllamaAsync(List<object> messages)
        {
            var apiUrl = _config["AI:OllamaUrl"] ?? "http://localhost:11434/api/chat" ?? throw new InvalidOperationException("AI:OllamaUrl is not configured.");
            var model = _config["AI:OllamaModel"] ?? "deepseek-r1:8b";

            var requestBody = new
            {
                model = model,
                messages = messages,
                stream = false

            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );
            try
            {
                var response = await _httpClient.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OllamaResponse>(responseBody);

                return result?.Message?.Content
                    ?? "I apologize, but I'm having trouble processing your request. Would you like to speak with a human agent?";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to connect to Ollama at {Url}", apiUrl);
                return "I'm having trouble connecting to the AI service. Please make sure Ollama is running.";
            }
        }


        public async Task RecordFeedbackAsync(string messageId, bool isHelpful)
        {
            _logger.LogInformation("Feedback for message {MessageId}: {IsHelpful}", messageId, isHelpful);
            await _conversationRepo.SaveFeedbackAsync(messageId, isHelpful);

        }

    }
}


