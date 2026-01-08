using SLA_API_AIChatBot_Poc.Model;

namespace SLA_API_AIChatBot_Poc.Interface
{
    public interface IChatService
    {
        Task<ChatServiceResponse> GetResponseAsync(string userMessage, string? conversationId, string? userId = null);
        Task RecordFeedbackAsync(string messageId, bool isHelpful);
        Task<bool> EscalateToHumanAsync(string conversationId);
    }
}
