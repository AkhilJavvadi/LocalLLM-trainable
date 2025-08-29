// This file contains the implementation of the MCP server and the tools.
using LocalLLM.Api.Services.Data;
using LocalLLM.Api.Services.Models;
using LocalLLM.Api.Services.Train;

namespace LocalLLM.Api.Services.Mcp;

/// <summary>
/// Minimal in-process “MCP-style” tool registry (HTTP callers go through controllers).
/// Replace with a real MCP SDK when you're ready.
/// </summary>
public sealed class McpServer
{
    private readonly Dictionary<string, IMcpTool> _tools = new();
    /// <summary>
    /// Registers a tool.
    /// </summary>
    /// <param name="tool">The tool to register.</param>
    public void Register(IMcpTool tool) => _tools[tool.Name] = tool;
    /// <summary>
    /// Calls a tool asynchronously.
    /// </summary>
    /// <param name="toolName">The name of the tool to call.</param>
    /// <param name="args">The arguments for the tool.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the tool invocation.</returns>
    public Task<object?> CallAsync(string toolName, Dictionary<string, object?> args, CancellationToken ct)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
            throw new InvalidOperationException($"Tool not found: {toolName}");
        return tool.InvokeAsync(args, ct);
    }
}

// Tool implementations
/// <summary>
/// Represents a tool for listing datasets.
/// </summary>
public sealed class DatasetListTool : IMcpTool
{
    private readonly IDatasetStore _store;
    /// <summary>
    /// Initializes a new instance of the <see cref="DatasetListTool"/> class.
    /// </summary>
    /// <param name="store">The dataset store.</param>
    public DatasetListTool(IDatasetStore store) => _store = store;
    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    public string Name => "dataset.list";
    /// <summary>
    /// Invokes the tool asynchronously.
    /// </summary>
    /// <param name="args">The arguments for the tool.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the tool invocation.</returns>
    public Task<object?> InvokeAsync(Dictionary<string, object?> _, CancellationToken __)
        => Task.FromResult<object?>(_store.List());
}
/// <summary>
/// Represents a tool for launching a trainer.
/// </summary>
public sealed class TrainerLaunchTool : IMcpTool
{
    private readonly ITrainer _trainer;
    /// <summary>
    /// Initializes a new instance of the <see cref="TrainerLaunchTool"/> class.
    /// </summary>
    /// <param name="trainer">The trainer.</param>
    public TrainerLaunchTool(ITrainer trainer) => _trainer = trainer;
    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    public string Name => "trainer.launch";
    /// <summary>
    /// Invokes the tool asynchronously.
    /// </summary>
    /// <param name="args">The arguments for the tool.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the tool invocation.</returns>
    public async Task<object?> InvokeAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        var req = new LaunchRequest(
            args["DatasetId"]?.ToString()!,
            args["BaseModel"]?.ToString() ?? "qwen2.5:0.5b",
            args.TryGetValue("Epochs", out var e) ? Convert.ToInt32(e) : 3,
            args.TryGetValue("LearningRate", out var lr) ? Convert.ToSingle(lr) : 2e-4f);
        var job = await _trainer.LaunchAsync(req, ct);
        return new { job.JobId, job.State, job.StartedAt };
    }
}
/// <summary>
/// Represents a tool for getting the status of a trainer.
/// </summary>
public sealed class TrainerStatusTool : IMcpTool
{
    private readonly ITrainer _trainer;
    /// <summary>
    /// Initializes a new instance of the <see cref="TrainerStatusTool"/> class.
    /// </summary>
    /// <param name="trainer">The trainer.</param>
    public TrainerStatusTool(ITrainer trainer) => _trainer = trainer;
    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    public string Name => "trainer.status";
    /// <summary>
    /// Invokes the tool asynchronously.
    /// </summary>
    /// <param name="args">The arguments for the tool.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the tool invocation.</returns>
    public Task<object?> InvokeAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        var jobId = args["JobId"]?.ToString()!;
        return Task.FromResult<object?>(_trainer.Status(jobId));
    }
}
/// <summary>
/// Represents a tool for loading a model.
/// </summary>
public sealed class ModelLoadTool : IMcpTool
{
    private readonly IModelRunner _runner;
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelLoadTool"/> class.
    /// </summary>
    /// <param name="runner">The model runner.</param>
    public ModelLoadTool(IModelRunner runner) => _runner = runner;
    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    public string Name => "model.load";
    /// <summary>
    /// Invokes the tool asynchronously.
    /// </summary>
    /// <param name="args">The arguments for the tool.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the tool invocation.</returns>
    public async Task<object?> InvokeAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        var model = args["Model"]?.ToString()!;
        await _runner.LoadAsync(model, ct);
        return new { Loaded = true, Model = model };
    }
}