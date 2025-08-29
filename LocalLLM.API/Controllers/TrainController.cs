// This file contains the TrainController class, which is responsible for handling training requests.
using LocalLLM.Api.Contracts;
using LocalLLM.Api.Services.Mcp;
using Microsoft.AspNetCore.Mvc;

namespace LocalLLM.Api.Controllers;
/// <summary>
/// The TrainController class, which is responsible for handling training requests.
/// </summary>
[ApiController]
[Route("api/train")]
public sealed class TrainController : ControllerBase
{
    /// <summary>
    /// Launches a training job.
    /// </summary>
    /// <param name="req">The training launch request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the training launch response.</returns>
    [HttpPost]
    public async Task<ActionResult<TrainLaunchResponse>> Launch([FromBody] TrainLaunchRequest req, CancellationToken ct)
    {
        var args = new Dictionary<string, object?>
        {
            ["DatasetId"]   = req.DatasetId,
            ["BaseModel"]   = string.IsNullOrWhiteSpace(req.BaseModel) ? "qwen2.5:0.5b" : req.BaseModel,
            ["Epochs"]      = req.Epochs ?? 3,
            ["LearningRate"]= req.LearningRate ?? 2e-4f
        };

        var result = await McpServerHostedService.Server.CallAsync("trainer.launch", args, ct);
        dynamic dyn = result!;
        return Ok(new TrainLaunchResponse((string)dyn.JobId, dyn.State.ToString(), (DateTime)dyn.StartedAt));
    }
    /// <summary>
    /// Gets the status of a training job.
    /// </summary>
    /// <param name="jobId">The ID of the training job.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the training status response.</returns>
    [HttpGet("{jobId}")]
    public async Task<ActionResult<TrainStatusResponse>> Status(string jobId, CancellationToken ct)
    {
        var result = await McpServerHostedService.Server.CallAsync("trainer.status", new() { ["JobId"] = jobId }, ct);
        dynamic dyn = result!;
        return Ok(new TrainStatusResponse((string)dyn.JobId, dyn.State.ToString(), (string)dyn.LogsTail, (string?)dyn.ArtifactPath));
    }
}