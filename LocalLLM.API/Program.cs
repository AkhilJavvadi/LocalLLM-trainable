// This file contains the main entry point for the application.
using LocalLLM.Api.Services.Data;
using LocalLLM.Api.Services.Mcp;
using LocalLLM.Api.Services.Models;
using LocalLLM.Api.Services.Train;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient();

// Controllers & JSON
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = null;
});

// CORS (dev-friendly)
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// App services
builder.Services.AddSingleton<IDatasetStore, LocalDatasetStore>();
builder.Services.AddSingleton<IModelRunner, OllamaRunner>();
builder.Services.AddSingleton<ITrainer, PythonTrainer>();


// Minimal MCP-style tool host
builder.Services.AddHostedService<McpServerHostedService>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors();

// Swagger UI at /docs
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LocalLLM API v1");
    c.RoutePrefix = "docs";
});

// Controllers
app.MapControllers();

// Redirect / to /docs
app.MapGet("/", () => Results.Redirect("/docs"));

app.Run();