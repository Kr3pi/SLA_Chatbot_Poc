using Microsoft.EntityFrameworkCore;
namespace SLA_API_AIChatBot_Poc.Model
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Conversations> ConversationsContext { get; set; }
        public DbSet<FeedbackRequest> Feedbacks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ConversationContext
            modelBuilder.Entity<Conversations>(entity =>
            {
                entity.HasKey(e => e.ConversationId);

                entity.Property(e => e.ConversationId)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.UserId)
                    .HasMaxLength(50);

                entity.Property(e => e.StartedAt)
                    .IsRequired();

                entity.Property(e => e.RequiresEscalation)
                    .IsRequired();

                // Store Messages as JSON
                entity.Property(e => e.MessagesJson)
                    .HasColumnName("MessagesJson")
                    .HasColumnType("nvarchar(max)");

                // Store Metadata as JSON
                entity.Property(e => e.MetadataJson)
                    .HasColumnName("MetadataJson")
                    .HasColumnType("nvarchar(max)");

                // Ignore the non-mapped properties
                entity.Ignore(e => e.Messages);
                entity.Ignore(e => e.Metadata);

                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.StartedAt);
            });

        }
    }
}
