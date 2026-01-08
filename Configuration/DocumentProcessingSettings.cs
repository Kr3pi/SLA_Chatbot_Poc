namespace SLA_API_AIChatBot_Poc.Configuration
{
    public class DocumentProcessingSettings
    {
        public string[] AllowedExtensions { get; set; } = new[] { ".pdf", ".docx", ".txt", ".md" };
        public int MaxFileSizeMB { get; set; } = 10;
        public string UploadPath { get; set; } = "uploads";
        public string ProcessedPath { get; set; } = "processed";
    }
}
