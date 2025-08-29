// This file contains the ModelController class, which is responsible for handling model registration requests.
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LocalLLM.Api.Controllers;
/// <summary>
/// The ModelController class, which is responsible for handling model registration requests.
/// </summary>
[ApiController]
[Route("api/model")]
public sealed class ModelController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelController"/> class.
    /// </summary>
    /// <param name="env">The web host environment.</param>
    public ModelController(IWebHostEnvironment env) => _env = env;
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
    /// <summary>
    /// Registers a model.
    /// </summary>
    /// <param name="req">The model registration request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the model registration response.</returns>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterModelResponse>> Register([FromBody] RegisterModelRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.JobId) || string.IsNullOrWhiteSpace(req.ModelName))
            return BadRequest("JobId and ModelName are required.");

        var artifactsDir = Path.Combine(_env.ContentRootPath, "runs", req.JobId, "artifacts");
        var modelfile = Path.Combine(artifactsDir, "Modelfile");
        if (!System.IO.File.Exists(modelfile))
            return NotFound($"Modelfile not found at: {modelfile}");

        // run: ollama create <name> -f Modelfile   (cwd = artifactsDir)
        var psi = new ProcessStartInfo
        {
            FileName = "ollama",
            Arguments = $"create {req.ModelName} -f \"{modelfile}\"",
            WorkingDirectory = artifactsDir,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = new Process { StartInfo = psi };
        proc.Start();
        var stdout = await proc.StandardOutput.ReadToEndAsync();
        var stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync(ct);

        var payload = new RegisterModelResponse
        {
            ModelName   = req.ModelName,
            ArtifactsDir= artifactsDir,
            Modelfile   = modelfile,
            ExitCode    = proc.ExitCode,
            Stdout      = stdout.Trim(),
            Stderr      = stderr.Trim()
        };

        if (proc.ExitCode != 0)
            return StatusCode(500, payload);

        return Ok(payload);
    }
}