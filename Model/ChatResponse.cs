namespace POC_SLAIS_Chat.Model
{
    public class ChatResponse
    {
        public string Reply { get; set; }
        public string ConversationId { get; set; }
        public bool RequiresEscalation { get; set; }
        public string Intent { get; set; }
    }
}
