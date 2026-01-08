using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SLA_API_AIChatBot_Poc.Configuration;
using SLA_API_AIChatBot_Poc.Interface;
using SLA_API_AIChatBot_Poc.Model;
using SLA_API_AIChatBot_Poc.Repository;
using SLA_API_AIChatBot_Poc.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("sqlConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null
        )
     );
});
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure settings
builder.Services.Configure<OllamaSettings>(
    builder.Configuration.GetSection("Ollama"));
builder.Services.Configure<VectorDatabaseSettings>(
    builder.Configuration.GetSection("VectorDatabase"));
builder.Services.Configure<RAGSettings>(
    builder.Configuration.GetSection("RAG"));
builder.Services.Configure<DocumentProcessingSettings>(
    builder.Configuration.GetSection("DocumentProcessing"));
builder.Services.Configure<ChatSettings>(
    builder.Configuration.GetSection("Chat"));
// ============================================
// 4. Core Chat Services
// ============================================
// Main chat orchestration service
// Register HttpClient for AI service calls
/*builder.Services.AddHttpClient<IChatService, ChatService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60); // Ollama can be slow on CPU
});*/
// Configure HttpClient for Ollama with timeout
builder.Services.AddHttpClient("Ollama", (serviceProvider, client) =>
{
    var ollamaSettings = serviceProvider.GetRequiredService<IOptions<OllamaSettings>>().Value;
    client.BaseAddress = new Uri(ollamaSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(ollamaSettings.RequestTimeout);
});
// Register the service
builder.Services.AddScoped<IOllamaService, OllamaService>();

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>()
            ?? new[] { "https://localhost:7136" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register chat service
builder.Services.AddScoped<IOllamaService, OllamaService>();
/*builder.Services.AddScoped<IVectorStoreService, QdrantService>();
builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
builder.Services.AddScoped<IRAGService, RAGService>();*/
/*builder.Services.AddScoped<IChatService, ChatService>();*/

// Add SignalR for real-time streaming (optional)
builder.Services.AddSignalR();

// Conversation storage and retrieval
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();      // Conversation storage
// Knowledge base for FAQs and documentation
/*builder.Services.AddScoped<KnowledgeBaseService, KnowledgeBaseService>();        // FAQ/docs access
*/var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
