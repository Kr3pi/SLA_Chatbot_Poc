namespace SLA_API_AIChatBot_Poc.Model
{
    public class ChatStreamChunk
    {
        public string? Content { get; set; }
        public bool Done { get; set; }
        public string? ConversationId { get; set; }
    }
}
