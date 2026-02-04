using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SLA_API_AIChatBot_Poc.Configuration;
using SLA_API_AIChatBot_Poc.Model;
using SLA_API_AIChatBot_Poc.Repository;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
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


builder.Services.Configure<DocumentProcessingSettings>(
    builder.Configuration.GetSection("DocumentProcessing"));



// CORS Configuration
builder.Services.AddCors(options =>
{
   
    options.AddPolicy("LocalDevPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:7061")
              .AllowAnyHeader()
              .AllowAnyMethod()
              // .AllowCredentials() // uncomment only if you actually send credentials
              ;
    });
});




// Add SignalR for real-time streaming (optional)
builder.Services.AddSignalR();

// Conversation storage and retrieval
/*builder.Services.AddScoped<IConversationRepository, ConversationRepository>();      // Conversation storage
// Knowledge base for FAQs and documentation*/
/*builder.Services.AddScoped<KnowledgeBaseService, KnowledgeBaseService>();        // FAQ/docs access
*/var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("LocalDevPolicy");
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
