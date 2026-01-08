namespace SLA_API_AIChatBot_Poc.Interface
{
    public interface IknowledgeBaseService
    {
        Task<string?> SearchAsync(string query, string intent);
    }
}
