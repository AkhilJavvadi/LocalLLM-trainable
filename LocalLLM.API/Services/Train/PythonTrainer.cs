// This file contains the implementation of the Python trainer.
using System.Collections.Concurrent;
using System.Diagnostics;
using LocalLLM.Api.Services.Data;

namespace LocalLLM.Api.Services.Train;
/// <summary>
/// Represents a Python trainer.
/// </summary>
public sealed class PythonTrainer : ITrainer
{
    private readonly IDatasetStore _datasets;
    private readonly IWebHostEnvironment _env;

    private sealed record RunState(Process Proc, string LogPath, string? ArtifactPath);
    private readonly ConcurrentDictionary<string, RunState> _runs = new();
    /// <summary>
    /// Initializes a new instance of the <see cref="PythonTrainer"/> class.
    /// </summary>
    /// <param name="datasets">The dataset store.</param>
    /// <param name="env">The web host environment.</param>
    public PythonTrainer(IDatasetStore datasets, IWebHostEnvironment env)
        => (_datasets, _env) = (datasets, env);

    /// <summary>
    /// Launches a training job asynchronously.
    /// </summary>
    /// <param name="request">The request to launch a training job.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the training job.</returns>
    public Task<TrainJob> LaunchAsync(LaunchRequest request, CancellationToken ct)
    {
        var jobId = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var runsDir = Path.Combine(_env.ContentRootPath, "runs", jobId);
        Directory.CreateDirectory(runsDir);

        var datasetPath = _datasets.GetPath(request.DatasetId);
        var logPath = Path.Combine(runsDir, "train.log");
        var outDir = Path.Combine(runsDir, "artifacts");
        Directory.CreateDirectory(outDir);

        var yaml = $$"""
        dataset_path: "{{datasetPath.Replace("\\", "/")}}"
        base_model: "{{request.BaseModel}}"
        epochs: {{request.Epochs}}
        learning_rate: {{request.LearningRate}}
        output_dir: "{{outDir.Replace("\\", "/")}}"
        """;
        File.WriteAllText(Path.Combine(runsDir, "config.yaml"), yaml);

        var psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"-u train.py --config \"{Path.Combine(runsDir, "config.yaml")}\"",
            WorkingDirectory = _env.ContentRootPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // ensure Python is unbuffered and UTF-8
        psi.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
        psi.StandardOutputEncoding = System.Text.Encoding.UTF8;
        psi.StandardErrorEncoding = System.Text.Encoding.UTF8;

        var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var logWriter = File.CreateText(logPath);
        logWriter.AutoFlush = true;

        proc.OutputDataReceived += (_, e) => { if (e.Data != null) logWriter.WriteLine(e.Data); logWriter.Flush(); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) logWriter.WriteLine(e.Data); logWriter.Flush(); };

        if (!proc.Start())
            throw new InvalidOperationException("Failed to start python training process.");

        // write pid for debugging
        File.WriteAllText(Path.Combine(runsDir, "pid.txt"), proc.Id.ToString());

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        _runs[jobId] = new RunState(proc, logPath, outDir);

        _ = Task.Run(async () =>
        {
            try { await proc.WaitForExitAsync(ct); }
            finally { logWriter.Flush(); logWriter.Dispose(); }
        }, ct);

        return Task.FromResult(new TrainJob(jobId, TrainState.Running, DateTime.UtcNow));
    }

    /// <summary>
    /// Gets the status of a training job.
    /// </summary>
    /// <param name="jobId">The unique identifier of the training job.</param>
    /// <returns>The status of the training job.</returns>
    public TrainerStatus Status(string jobId)
    {
        if (!_runs.TryGetValue(jobId, out var state))
            return new TrainerStatus(jobId, TrainState.Failed, "Unknown job.", null);

        var proc = state.Proc;
        var runState = proc.HasExited
            ? (proc.ExitCode == 0 ? TrainState.Succeeded : TrainState.Failed)
            : TrainState.Running;

        var tail = Tail(state.LogPath, 16384);
        return new TrainerStatus(jobId, runState, tail, state.ArtifactPath);
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
            if (!File.Exists(path)) return "";
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            var len = fs.Length;
            var toRead = (int)Math.Min(maxBytes, len);
            fs.Seek(-toRead, SeekOrigin.End);
            using var sr = new StreamReader(fs);
            return sr.ReadToEnd();
        }
        catch { return ""; }
    }
}