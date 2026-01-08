namespace SLA_API_AIChatBot_Poc.Model
{
    public class FeedbackRequest
    {
        public int Id { get; set; }
        public string MessageId { get; set; }
        public bool IsHelpful { get; set; }

        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
