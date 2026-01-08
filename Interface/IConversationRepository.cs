using SLA_API_AIChatBot_Poc.Model;

namespace SLA_API_AIChatBot_Poc.Interface
{
    public interface IConversationRepository
    {
        // ============================================
        // Conversation Repository - Stores conversation history
        // ============================================
        Task<Conversations?> GetConversationAsync(string conversationId);
        Task SaveConversationAsync(Conversations context);
        Task SaveFeedbackAsync(string messageId, bool isHelpful);
    }
}
