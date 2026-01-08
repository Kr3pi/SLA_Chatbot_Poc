namespace SLA_API_AIChatBot_Poc.Configuration
{
    public class OllamaSettings
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string ChatModel { get; set; } = "deepseek-llm";
        public string EmbeddingModel { get; set; } = "nomic-embed-text";
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 2000;
        public int ContextWindow { get; set; } = 4096;
        public int RequestTimeout { get; set; } = 300;
        public bool StreamingEnabled { get; set; } = true;
    }
}
