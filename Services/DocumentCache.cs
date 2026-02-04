namespace SLA_API_AIChatBot_Poc.Services
{
    public static class DocumentCache
    {
        private static readonly Dictionary<string, string> _cache = new Dictionary<string, string>();

        public static void AddOrUpdate(string docId, string content)
        {
            _cache[docId] = content;
        }
        public static string? Get(string docId)
        {
            return _cache.TryGetValue(docId, out var content) ? content : null;
        }

    }
}
