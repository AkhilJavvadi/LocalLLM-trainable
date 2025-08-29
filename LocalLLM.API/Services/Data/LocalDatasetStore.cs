// This file contains the implementation of the dataset store.
using System.Text.Json;

namespace LocalLLM.Api.Services.Data;
/// <summary>
/// Represents a local dataset store.
/// </summary>
public sealed class LocalDatasetStore : IDatasetStore
{
    private readonly string _root;
    private readonly List<DatasetInfo> _cache = new();
    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDatasetStore"/> class.
    /// </summary>
    /// <param name="env">The web host environment.</param>
    public LocalDatasetStore(IWebHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, "datasets");
        Directory.CreateDirectory(_root);

        // Load index if present
        var indexPath = Path.Combine(_root, "index.json");
        if (File.Exists(indexPath))
        {
            var json = File.ReadAllText(indexPath);
            var items = JsonSerializer.Deserialize<List<DatasetInfo>>(json);
            if (items is not null) _cache.AddRange(items);
        }
    }
    /// <summary>
    /// Adds a dataset to the store.
    /// </summary>
    /// <param name="file">The stream of the dataset file.</param>
    /// <param name="fileName">The name of the dataset file.</param>
    /// <returns>The information about the added dataset.</returns>
    public DatasetInfo Add(Stream file, string fileName)
    {
        var id = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var dir = Path.Combine(_root, id);
        Directory.CreateDirectory(dir);

        var dst = Path.Combine(dir, Path.GetFileName(fileName));
        using (var fs = File.Create(dst)) { file.CopyTo(fs); }

        // Optional: normalize to JSONL with fields {instruction,input,output}
        // For demo, we just count lines if .jsonl
        var count = 0;
        if (dst.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
            count = File.ReadLines(dst).Count();

        var info = new DatasetInfo(id, dst, count, DateTime.UtcNow);
        _cache.Add(info);
        PersistIndex();
        return info;
    }
    /// <summary>
    /// Lists all the datasets in the store.
    /// </summary>
    /// <returns>A read-only list of dataset information.</returns>
    public IReadOnlyList<DatasetInfo> List() => _cache;
    /// <summary>
    /// Gets the path of a dataset.
    /// </summary>
    /// <param name="datasetId">The unique identifier of the dataset.</param>
    /// <returns>The path to the dataset file.</returns>
    public string GetPath(string datasetId)
    {
        var found = _cache.FirstOrDefault(x => x.Id == datasetId)
            ?? throw new InvalidOperationException("Dataset not found");
        return found.Path;
    }
    /// <summary>
    /// Persists the index of the datasets.
    /// </summary>
    private void PersistIndex()
    {
        var indexPath = Path.Combine(_root, "index.json");
        File.WriteAllText(indexPath, System.Text.Json.JsonSerializer.Serialize(_cache));
    }
}