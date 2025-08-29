// This file contains the data transfer objects for models.
namespace LocalLLM.Api.Contracts;
/// <summary>
/// Represents a request to register a model.
/// </summary>
public sealed class RegisterModelRequest
{
    /// <summary>
    /// The ID of the job that created the model.
    /// </summary>
    public string JobId { get; set; } = default!;
    /// <summary>
    /// The name of the model.
    /// </summary>
    public string ModelName { get; set; } = default!;
}
/// <summary>
/// Represents a response to a model registration request.
/// </summary>
public sealed class RegisterModelResponse
{
    /// <summary>
    /// The name of the model.
    /// </summary>
    public string ModelName { get; init; } = default!;
    /// <summary>
    /// The directory where the model artifacts are stored.
    /// </summary>
    public string ArtifactsDir { get; init; } = default!;
    /// <summary>
    /// The path to the Modelfile.
    /// </summary>
    public string Modelfile { get; init; } = default!;
    /// <summary>
    /// The exit code of the model registration process.
    /// </summary>
    public int ExitCode { get; init; }
    /// <summary>
    /// The standard output of the model registration process.
    /// </summary>
    public string Stdout { get; init; } = "";
    /// <summary>
    /// The standard error of the model registration process.
    /// </summary>
    public string Stderr { get; init; } = "";
}