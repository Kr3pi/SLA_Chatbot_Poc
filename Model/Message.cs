namespace SLA_API_AIChatBot_Poc.Model
{
    public class Message
    {
        public string? MessageId { get; set; }
        public string Role { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
