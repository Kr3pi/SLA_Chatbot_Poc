using Microsoft.EntityFrameworkCore;
using SLA_API_AIChatBot_Poc.Interface;
using SLA_API_AIChatBot_Poc.Model;

namespace SLA_API_AIChatBot_Poc.Repository
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly AppDbContext _context; // DbContext
        private readonly ILogger<ConversationRepository> _logger;

        public ConversationRepository(AppDbContext context, ILogger<ConversationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Conversations?> GetConversationAsync(string conversationId)
        {
            try
            {
                _logger.LogInformation("Retrieving conversation {ConversationId}", conversationId);

                var conversation = await _context.ConversationsContext
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

                if (conversation != null)
                {
                    _logger.LogInformation("Found conversation {ConversationId} with {MessageCount} messages",
                        conversationId, conversation.Messages?.Count ?? 0);
                }
                else
                {
                    _logger.LogInformation("Conversation {ConversationId} not found", conversationId);
                }

                return conversation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversation {ConversationId}", conversationId);
                return null;
            }
        }

        public async Task SaveConversationAsync(Conversations context)
        {
            try
            {
                _logger.LogInformation("Saving conversation {ConversationId}", context.ConversationId);

                var existingConversation = await _context.ConversationsContext
                    .FirstOrDefaultAsync(c => c.ConversationId == context.ConversationId);

                if (existingConversation != null)
                {
                    // Update existing conversation
                    _context.Entry(existingConversation).State = EntityState.Detached;
                    _context.ConversationsContext.Update(context);
                    _logger.LogInformation("Updating existing conversation {ConversationId}", context.ConversationId);
                }
                else
                {
                    // Add new conversation
                    await _context.ConversationsContext.AddAsync(context);
                    _logger.LogInformation("Adding new conversation {ConversationId}", context.ConversationId);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully saved conversation {ConversationId}", context.ConversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving conversation {ConversationId}", context.ConversationId);
                throw;
            }
        }

        public async Task SaveFeedbackAsync(string messageId, bool isHelpful)
        {
            try
            {
                _logger.LogInformation("Saving feedback for message {MessageId}: {IsHelpful}",
                    messageId, isHelpful);

                var feedback = new FeedbackRequest
                {
                    MessageId = messageId,
                    IsHelpful = isHelpful,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Feedbacks.AddAsync(feedback);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully saved feedback for message {MessageId}", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving feedback for message {MessageId}", messageId);
                throw;
            }
        }

        // Additional helper methods

        public async Task<List<Conversations>> GetUserConversationsAsync(string userId)
        {
            return await _context.ConversationsContext
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.StartedAt)
                .ToListAsync();
        }

        public async Task<bool> DeleteConversationAsync(string conversationId)
        {
            try
            {
                var conversation = await _context.ConversationsContext
                    .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

                if (conversation == null)
                    return false;

                _context.ConversationsContext.Remove(conversation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted conversation {ConversationId}", conversationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation {ConversationId}", conversationId);
                return false;
            }
        }
    }

}

