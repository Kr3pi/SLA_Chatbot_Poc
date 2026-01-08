namespace POC_SLAIS_Chat.Model
{
    public class ChatRequest
    {
        public string Message { get; set; }
        public string? ConversationId { get; set; }
        public string? UserId { get; set; }
    }
}
