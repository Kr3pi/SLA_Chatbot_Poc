using SLA_API_AIChatBot_Poc.Model;
using System.Runtime.CompilerServices;

namespace SLA_API_AIChatBot_Poc.Interface
{
    public interface IOllamaService
    {
        /// <summary>
        /// Generate a chat response (non-streaming)
        /// </summary>
        Task<string> GenerateChatResponseAsync(string prompt,List<Message>? conversationHistory = null,
            CancellationToken cancellationToken = default);


        /// <summary>
        /// Generate a chat response with streaming
        /// </summary>
        IAsyncEnumerable<string> GenerateChatResponseStreamAsync(string prompt, List<Message>? conversationHistory = null, [EnumeratorCancellation] CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate embeddings for a given text
        /// </summary>
        Task<float[]> GenerateEmbeddingAsync(string text);

        /// <summary>
        /// Generate embeddings for multiple texts in batch
        /// </summary>
        Task<List<float[]>> GenerateEmbeddingsBatchAsync(List<string> texts);

        /// <summary>
        /// Generate a response with RAG context
        /// </summary>
        Task<string> GenerateRAGResponseAsync(string query, List<string> retrievedContexts, List<Message>? conversationHistory = null);

        /// <summary>
        /// Generate a streaming response with RAG context
        /// </summary>
        IAsyncEnumerable<string> GenerateRAGResponseStreamAsync(string query, List<string> retrievedContexts, List<Message>? conversationHistory = null);

        /// <summary>
        /// Check if Ollama service is available
        /// </summary>
        Task<bool> IsServiceAvailableAsync();

        /// <summary>
        /// List available models
        /// </summary>
        Task<List<string>> ListModelsAsync();
    }
}

