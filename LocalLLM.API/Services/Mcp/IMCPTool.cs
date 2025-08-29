// This file contains the interface for the MCP tool.
namespace LocalLLM.Api.Services.Mcp;
/// <summary>
/// Represents the interface for an MCP tool.
/// </summary>
public interface IMcpTool
{
    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    string Name { get; }
    /// <summary>
    /// Invokes the tool asynchronously.
    /// </summary>
    /// <param name="args">The arguments for the tool.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the tool invocation.</returns>
    Task<object?> InvokeAsync(Dictionary<string, object?> args, CancellationToken ct);
}