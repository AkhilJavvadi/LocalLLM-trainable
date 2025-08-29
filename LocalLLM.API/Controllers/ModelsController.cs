// This file contains the ModelsController class, which is responsible for handling model list requests.
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalLLM.Api.Controllers;
/// <summary>
/// The ModelsController class, which is responsible for handling model list requests.
/// </summary>
[ApiController]
[Route("api/models")]
public sealed class ModelsController : ControllerBase
{
    private readonly IHttpClientFactory _http;
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelsController"/> class.
    /// </summary>
    /// <param name="http">The HTTP client factory.</param>
    public ModelsController(IHttpClientFactory http) => _http = http;
    /// <summary>
    /// Lists all models.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of models.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<string>>> List(CancellationToken ct)
    {
        var names = new List<string>();

        // --- 1) Try Ollama's HTTP API (preferred) ---
        try
        {
            var client = _http.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            // GET http://127.0.0.1:11434/api/tags  ->  { "models": [ { "name": "...", "model": "..." }, ... ] }
            using var resp = await client.GetAsync("http://127.0.0.1:11434/api/tags", ct);
            if (resp.IsSuccessStatusCode)
            {
                using var stream = await resp.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                if (doc.RootElement.TryGetProperty("models", out var models) && models.ValueKind == JsonValueKind.Array)
                {
                    foreach (var m in models.EnumerateArray())
                    {
                        string? name = null;
                        if (m.TryGetProperty("name", out var n)) name = n.GetString();
                        if (string.IsNullOrWhiteSpace(name) && m.TryGetProperty("model", out var mm)) name = mm.GetString();
                        if (!string.IsNullOrWhiteSpace(name)) names.Add(name!);
                    }
                }
            }
        }
        catch { /* ignore and fall back */ }

        // --- 2) Fallback: CLI `ollama list --json` ---
        if (names.Count == 0)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ollama",
                    Arguments = "list --json",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = new Process { StartInfo = psi };
                p.Start();
                var stdout = await p.StandardOutput.ReadToEndAsync();
                _ = await p.StandardError.ReadToEndAsync();
                await p.WaitForExitAsync(ct);

                // Parse NDJSON: each line is a JSON object with "name"
                foreach (var line in stdout.Split('\n'))
                {
                    var t = line.Trim();
                    if (string.IsNullOrEmpty(t)) continue;
                    try
                    {
                        using var doc = JsonDocument.Parse(t);
                        if (doc.RootElement.TryGetProperty("name", out var nameProp))
                        {
                            var n = nameProp.GetString();
                            if (!string.IsNullOrWhiteSpace(n)) names.Add(n!);
                        }
                    }
                    catch { /* ignore */ }
                }

                // Older ollama prints a table; take first column
                if (names.Count == 0)
                {
                    foreach (var line in stdout.Split('\n'))
                    {
                        var t = line.Trim();
                        if (string.IsNullOrEmpty(t) || t.StartsWith("NAME", StringComparison.OrdinalIgnoreCase)) continue;
                        var first = Regex.Split(t, "\\s+").FirstOrDefault();
                        if (!string.IsNullOrEmpty(first)) names.Add(first);
                    }
                }
            }
            catch { /* swallow */ }
        }

        // Dedup & sort; if still empty, return empty array (frontend can fallback)
        names = names.Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                     .ToList();

        return Ok(names);
    }
}