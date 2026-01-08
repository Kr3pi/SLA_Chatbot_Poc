using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using POC_SLAIS_Chat.Model;
using SLA_API_AIChatBot_Poc.Interface;
using SLA_API_AIChatBot_Poc.Model;

namespace SLA_API_AIChatBot_Poc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "Message cannot be empty" });
                }

                var response = await _chatService.GetResponseAsync(
                    request.Message,
                    request.ConversationId,
                    request.UserId
                 );

                return Ok(new ChatResponse
                {
                    Reply = response.Message,
                    ConversationId = response.ConversationId,
                    RequiresEscalation = response.RequiresEscalation,
                    Intent = response.Intent
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                return StatusCode(500, new { error = "An error occurred. Make sure Ollama is running." });
            }
        }



        [HttpPost("feedback")]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackRequest request)
        {
            try
            {
                await _chatService.RecordFeedbackAsync(request.MessageId, request.IsHelpful);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording feedback");
                return StatusCode(500, new { error = "Failed to record feedback" });
            }
        }
    }
}

