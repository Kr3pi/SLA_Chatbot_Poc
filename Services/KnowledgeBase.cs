using SLA_API_AIChatBot_Poc.Model;
using System.Formats.Tar;
using System.Net;
using System.Text.Json;

namespace SLA_API_AIChatBot_Poc.Services
{
    public class KnowledgeBase
    {
        // private static Dictionary<string, string> _qa = new();
        private static List<QAEntry> _qa = new();
        private static string _filePath = Path.Combine(AppContext.BaseDirectory, "KnowledgeBase.json");
        public static void Load(string filePath)
        {
            _filePath = filePath;
            if (!File.Exists(filePath)) return;

            var json = File.ReadAllText(filePath);
            var doc = JsonSerializer.Deserialize<Dictionary<string, List<QAEntry>>>(json);

            if (doc != null && doc.ContainsKey("KnowledgeBase"))
            {
                _qa = doc["KnowledgeBase"];
            }
        }

        public static QAEntry? GetAnswer(string userQuestion)
        { // Exact match
            var exact = _qa.FirstOrDefault(x => x.Question.Equals(userQuestion, StringComparison.OrdinalIgnoreCase));

            if (exact != null)
                return exact;

            // Fuzzy match
            var bestMatch = FuzzySharp.Process.ExtractOne(userQuestion, _qa.Select(x => x.Question));

            if (bestMatch != null && bestMatch.Score > 70)
            {
                return _qa.FirstOrDefault(x => x.Question == bestMatch.Value);
            }
            return null;
        }

        public static void AddNewQA(string question, string answer, string source = "JARVIS")
        {
            _qa.Add(new QAEntry
            {
                Question = question,
                Answer = answer,
                Source = source
            });

            var wrapper = new { RoadTransportActQA = _qa };
            var json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        /*   public static void AddNewQA(string question, string answer)
           {
               _qa[question] = answer;

               // Persist back to JSON
               var qaList = _qa.Select(kvp => new { question = kvp.Key, answer = kvp.Value }).ToList();
               var wrapper = new { RoadTransportActQA = qaList };
               var json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions { WriteIndented = true });
               File.WriteAllText(_filePath, json);

           }*/
    }
}
