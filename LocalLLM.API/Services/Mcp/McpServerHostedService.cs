// This file contains the implementation of the MCP server hosted service.
using LocalLLM.Api.Services.Data;
using LocalLLM.Api.Services.Models;
using LocalLLM.Api.Services.Train;

namespace LocalLLM.Api.Services.Mcp;
/// <summary>
/// Represents a hosted service for the MCP server.
/// </summary>
public sealed class McpServerHostedService : BackgroundService
{
    /// <summary>
    /// Gets the MCP server.
    /// </summary>
    public static McpServer Server { get; } = new();

    private readonly IDatasetStore _datasets;
    private readonly ITrainer _trainer;
    private readonly IModelRunner _runner;
    /// <summary>
    /// Initializes a new instance of the <see cref="McpServerHostedService"/> class.
    /// </summary>
    /// <param name="datasets">The dataset store.</param>
    /// <param name="trainer">The trainer.</param>
    /// <param name="runner">The model runner.</param>
    public McpServerHostedService(IDatasetStore datasets, ITrainer trainer, IModelRunner runner)
        => (_datasets, _trainer, _runner) = (datasets, trainer, runner);
    /// <summary>
    /// Executes the hosted service.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Register tools once
        Server.Register(new DatasetListTool(_datasets));
        Server.Register(new TrainerLaunchTool(_trainer));
        Server.Register(new TrainerStatusTool(_trainer));
        Server.Register(new ModelLoadTool(_runner));
        return Task.CompletedTask;
    }
}