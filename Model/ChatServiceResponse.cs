namespace SLA_API_AIChatBot_Poc.Model
{
    public class ChatServiceResponse
    {
        public string Message { get; set; }
        public string ConversationId { get; set; }
        public bool RequiresEscalation { get; set; }
        public string Intent { get; set; }
        public double Confidence { get; set; }
    }
}
