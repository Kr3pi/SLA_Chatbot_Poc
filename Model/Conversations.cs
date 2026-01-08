using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SLA_API_AIChatBot_Poc.Model
{
    public class Conversations
    {
        [Key]
        [MaxLength(50)]
        public string ConversationId { get; set; }

        [MaxLength(50)]
        public string? UserId { get; set; }

        public DateTime StartedAt { get; set; }

        public bool RequiresEscalation { get; set; }

        // JSON storage properties (mapped to database)
        [Column("MessagesJson")]
        public string MessagesJson
        {
            get => Messages != null ? JsonSerializer.Serialize(Messages) : "[]";
            set => Messages = string.IsNullOrEmpty(value)
                ? new List<Message>()
                : JsonSerializer.Deserialize<List<Message>>(value) ?? new List<Message>();
        }

        [Column("MetadataJson")]
        public string MetadataJson
        {
            get => Metadata != null ? JsonSerializer.Serialize(Metadata) : "{}";
            set => Metadata = string.IsNullOrEmpty(value)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(value) ?? new Dictionary<string, object>();
        }

        // In-memory properties (not mapped to database)
        [NotMapped]
        public List<Message> Messages { get; set; } = new List<Message>();

        [NotMapped]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public void AddMessage(string role, string content)
        {
            Messages.Add(new Message
            {
                Role = role,
                Content = content,
                Timestamp = DateTime.UtcNow,
                MessageId = Guid.NewGuid().ToString()
            });
        }

    } }
