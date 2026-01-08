namespace SLA_API_AIChatBot_Poc.Configuration
{
    public class RAGSettings
    {
        public int ChunkSize { get; set; } = 800;
        public int ChunkOverlap { get; set; } = 200;
        public int TopK { get; set; } = 5;
        public double MinRelevanceScore { get; set; } = 0.7;
        public int MaxContextLength { get; set; } = 3000;
        public bool EnableReranking { get; set; } = false;
    }
}
