// This file contains the interface for the model runner.
namespace LocalLLM.Api.Services.Models;
/// <summary>
/// Represents the interface for a model runner.
/// </summary>
public interface IModelRunner
{
    /// <summary>
    /// Loads a model asynchronously.
    /// </summary>
    /// <param name="model">The name of the model to load.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task LoadAsync(string model, CancellationToken ct);
    /// <summary>
    /// Generates a response from a model asynchronously.
    /// </summary>
    /// <param name="model">The name of the model to use.</param>
    /// <param name="prompt">The prompt to generate a response from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An asynchronous enumerable of strings representing the generated response.</returns>
    IAsyncEnumerable<string> GenerateAsync(string model, string prompt, CancellationToken ct);
}