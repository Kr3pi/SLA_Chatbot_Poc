namespace SLA_API_AIChatBot_Poc.Configuration
{
    public class ChatSettings
    {
        public int MaxConversationHistory { get; set; } = 10;
        public int ConversationTimeoutMinutes { get; set; } = 30;
        public bool EnableConversationLogging { get; set; } = true;
    }
}
