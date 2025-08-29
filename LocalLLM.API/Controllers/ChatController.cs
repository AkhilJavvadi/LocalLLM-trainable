// This file contains the ChatController class, which is responsible for handling chat requests.
using LocalLLM.Api.Contracts;
using LocalLLM.Api.Services.Mcp;
using LocalLLM.Api.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace LocalLLM.Api.Controllers;
/// <summary>
/// The ChatController class, which is responsible for handling chat requests.
/// </summary>
[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    /// <summary>
    /// Handles a chat request and streams the response.
    /// </summary>
    /// <param name="req">The chat request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [HttpPost]
    public async Task Stream([FromBody] ChatRequest req, CancellationToken ct)
    {
        try
        {
            var model = string.IsNullOrWhiteSpace(req.Model) ? "qwen2.5:0.5b" : req.Model;

            // Ensure model present/selected (now resilient to local models)
            await McpServerHostedService.Server.CallAsync("model.load", new() { ["Model"] = model }, ct);

            Response.ContentType = "text/plain; charset=utf-8";
            Response.StatusCode = 200;

            var runner = HttpContext.RequestServices.GetRequiredService<IModelRunner>();
            await foreach (var token in runner.GenerateAsync(model, req.Prompt, ct))
            {
                await Response.WriteAsync(token, ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (Exception ex)
        {
            if (!Response.HasStarted)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                Response.ContentType = "text/plain; charset=utf-f";
            }
            await Response.WriteAsync($"ERROR: {ex.Message}");
        }
    }
}