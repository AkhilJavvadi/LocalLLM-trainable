// This file contains the implementation of the Ollama runner.
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Diagnostics;
using OllamaSharp;
using OllamaSharp.Models.Exceptions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LocalLLM.Api.Services.Models;
/// <summary>
/// Represents an Ollama runner.
/// </summary>
public sealed class OllamaRunner : IModelRunner
{
    private readonly string _ollamaBase;
    private readonly HttpClient _http;
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaRunner"/> class.
    /// </summary>
    /// <param name="cfg">The configuration.</param>
    public OllamaRunner(IConfiguration cfg)
    {
        _ollamaBase = (cfg["Ollama:Endpoint"] ?? "http://127.0.0.1:11434").TrimEnd('/');
        _http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
    }

    // Ensure the model is available locally; if not, pull it.
    /// <summary>
    /// Loads a model asynchronously.
    /// </summary>
    /// <param name="model">The name of the model to load.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task LoadAsync(string model, CancellationToken ct)
    {
        if (await ModelExistsAsync(model, ct)) return;

        // POST /api/pull (streams NDJSON)
        var payload = new { name = model };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_ollamaBase}/api/pull")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;
            using var doc = JsonDocument.Parse(line);

            // If the daemon returns an error mid-stream, throw
            if (doc.RootElement.TryGetProperty("error", out var err))
                throw new InvalidOperationException(err.GetString());

            // Some versions emit { "status":"success" } at the end
            if (doc.RootElement.TryGetProperty("status", out var st) &&
                string.Equals(st.GetString(), "success", StringComparison.OrdinalIgnoreCase))
                break;
        }
    }
    /// <summary>
    /// Generates a response from a model asynchronously.
    /// </summary>
    /// <param name="model">The name of the model to use.</param>
    /// <param name="prompt">The prompt to generate a response from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An asynchronous enumerable of strings representing the generated response.</returns>
    public async IAsyncEnumerable<string> GenerateAsync(
        string model,
        string prompt,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var payload = new
        {
            model = model,
            prompt = prompt,
            stream = true,
            options = new
            {
                num_predict = 256,        // cap output so it ends
                temperature = 0.3,
                stop = new[]              // stop when it tries to start a new turn
                {
                    "\nUser:", "User:",
                    "\nAssistant:", "Assistant:"
                }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_ollamaBase}/api/generate")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? line;
        int emitted = 0;
        const int hardCharCap = 6000; // extra safety

        while ((line = await reader.ReadLineAsync()) is not null)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;

            using var doc = JsonDocument.Parse(line);

            if (doc.RootElement.TryGetProperty("response", out var r))
            {
                var chunk = r.GetString();
                if (!string.IsNullOrEmpty(chunk))
                {
                    emitted += chunk.Length;
                    yield return chunk;
                    if (emitted >= hardCharCap) yield break;
                }
            }

            if (doc.RootElement.TryGetProperty("done", out var doneEl) && doneEl.GetBoolean())
                yield break;
        }
    }
    /// <summary>
    /// Checks if a model exists asynchronously.
    /// </summary>
    /// <param name="model">The name of the model to check.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the model exists.</returns>
    private async Task<bool> ModelExistsAsync(string model, CancellationToken ct)
    {
        using var resp = await _http.GetAsync($"{_ollamaBase}/api/tags", ct);
        if (!resp.IsSuccessStatusCode) return false;

        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (doc.RootElement.TryGetProperty("models", out var models) &&
            models.ValueKind == JsonValueKind.Array)
        {
            foreach (var m in models.EnumerateArray())
            {
                string? name = null;
                if (m.TryGetProperty("name", out var n)) name = n.GetString();
                if (string.Equals(name, model, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }
}