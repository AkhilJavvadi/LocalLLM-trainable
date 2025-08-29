// This file contains the data transfer objects for chat.
namespace LocalLLM.Api.Contracts;
/// <summary>
/// Represents a chat request.
/// </summary>
/// <param name="Prompt">The prompt for the chat.</param>
/// <param name="Model">The model to use for the chat.</param>
public record ChatRequest(string Prompt, string? Model);