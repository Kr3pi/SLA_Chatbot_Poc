using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SLA_API_AIChatBot_Poc.Interface;
using SLA_API_AIChatBot_Poc.Model;

namespace SLA_API_AIChatBot_Poc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConversationRepository _repo;

        public TestController(AppDbContext context, IConversationRepository repo)
        {
            _context = context;
            _repo = repo;
        }

        [HttpGet("database")]
        public async Task<IActionResult> TestDatabase()
        {
            // Test database connection
            var canConnect = await _context.Database.CanConnectAsync();

            return Ok(new
            {
                databaseConnected = canConnect,
                conversationCount = await _context.ConversationsContext.CountAsync()
            });
        }

        [HttpGet("repository")]
        public async Task<IActionResult> TestRepository()
        {
            // Test repository
            var testConversation = new Conversations
            {
                ConversationId = $"test-{Guid.NewGuid()}",
                UserId = "test-user",
                StartedAt = DateTime.UtcNow
            };

            testConversation.AddMessage("user", "Hello!");
            testConversation.AddMessage("assistant", "Hi there!");

            await _repo.SaveConversationAsync(testConversation);

            var retrieved = await _repo.GetConversationAsync(testConversation.ConversationId);

            return Ok(new
            {
                saved = true,
                retrieved = retrieved != null,
                messageCount = retrieved?.Messages.Count ?? 0
            });
        }
    }
}
