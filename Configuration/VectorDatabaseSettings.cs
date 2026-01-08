namespace SLA_API_AIChatBot_Poc.Configuration
{
    public class VectorDatabaseSettings
    {
        public string Type { get; set; } = "Qdrant";
        public string ConnectionString { get; set; } = "http://localhost:6333";
        public string CollectionName { get; set; } = "documents";
        public int VectorSize { get; set; } = 768;
        public string Distance { get; set; } = "Cosine";
    }
}
