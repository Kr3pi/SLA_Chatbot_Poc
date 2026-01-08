using Microsoft.EntityFrameworkCore;
using POC_SLAIS_Chat.Service;
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
// ============================================
// 4. Core Chat Services
// ============================================
// Main chat orchestration service
// Register HttpClient for AI service calls
builder.Services.AddHttpClient<IChatService, ChatService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60); // Ollama can be slow on CPU
});
// Register chat service
builder.Services.AddScoped<IChatService, ChatService>();

// Conversation storage and retrieval
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();      // Conversation storage
// Knowledge base for FAQs and documentation
builder.Services.AddScoped<KnowledgeBaseService, KnowledgeBaseService>();        // FAQ/docs access
var app = builder.Build();

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
