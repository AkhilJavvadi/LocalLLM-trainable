// This file contains the TrainRecoverController class, which is responsible for handling training recovery requests.
using Microsoft.AspNetCore.Mvc;

namespace LocalLLM.Api.Controllers;
/// <summary>
/// The TrainRecoverController class, which is responsible for handling training recovery requests.
/// </summary>
[ApiController]
[Route("api/train")]
public sealed class TrainRecoverController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    /// <summary>
    /// Initializes a new instance of the <see cref="TrainRecoverController"/> class.
    /// </summary>
    /// <param name="env">The web host environment.</param>
    public TrainRecoverController(IWebHostEnvironment env) => _env = env;

    // Fallback status: read files from runs/<jobId> even if the in-memory registry was lost
    /// <summary>
    /// Recovers a training job.
    /// </summary>
    /// <param name="jobId">The ID of the training job to recover.</param>
    /// <returns>An object containing the recovered training job information.</returns>
    [HttpGet("recover/{jobId}")]
    public ActionResult<object> Recover(string jobId)
    {
        var runsDir = Path.Combine(_env.ContentRootPath, "runs", jobId);
        var artifactDir = Path.Combine(runsDir, "artifacts");
        var modelFile = Path.Combine(artifactDir, "Modelfile");
        var logPath = Path.Combine(runsDir, "train.log");

        if (!Directory.Exists(runsDir))
            return Ok(new { jobId, state = "Failed", logsTail = "Unknown job (no runs folder)", artifactPath = (string?)null });

        var state = System.IO.File.Exists(modelFile) ? "Succeeded" : "Running";
        var logs = Tail(logPath, 8192);
        var artifactPath = Directory.Exists(artifactDir) ? artifactDir : null;

        return Ok(new { jobId, state, logsTail = logs, artifactPath });
    }
    /// <summary>
    /// Tails a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="maxBytes">The maximum number of bytes to read.</param>
    /// <returns>The tail of the file.</returns>
    private static string Tail(string path, int maxBytes)
    {
        try
        {
            if (!System.IO.File.Exists(path)) return "";
            var fi = new FileInfo(path);
            var size = (int)Math.Min(maxBytes, fi.Length);
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fs.Seek(-size, SeekOrigin.End);
            using var sr = new StreamReader(fs);
            return sr.ReadToEnd();
        }
        catch { return ""; }
    }
}