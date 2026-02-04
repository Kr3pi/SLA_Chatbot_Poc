using System.Text.Json.Serialization;

namespace SLA_API_AIChatBot_Poc.Model
{
    public class OllamaResponse
    {
        public string Model { get; set; }
        public DateTime Created_At { get; set; }
        public string Response { get; set; }
        public bool Done { get; set; }
        public string Done_Reason { get; set; }
        public List<long> Context { get; set; }
        public long Total_Duration { get; set; }
        public long Load_Duration { get; set; }
        public int Prompt_Eval_Count { get; set; }
        public long Prompt_Eval_Duration { get; set; }
        public int Eval_Count { get; set; }
        public long Eval_Duration { get; set; }
    }
}
