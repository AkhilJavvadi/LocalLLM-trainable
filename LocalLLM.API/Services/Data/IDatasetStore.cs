// This file contains the interface for the dataset store and the record for the dataset information.
namespace LocalLLM.Api.Services.Data;
/// <summary>
/// Represents information about a dataset.
/// </summary>
/// <param name="Id">The unique identifier of the dataset.</param>
/// <param name="Path">The path to the dataset file.</param>
/// <param name="Count">The number of records in the dataset.</param>
/// <param name="CreatedAt">The date and time the dataset was created.</param>
public record DatasetInfo(string Id, string Path, int Count, DateTime CreatedAt);
/// <summary>
/// Represents the interface for a dataset store.
/// </summary>
public interface IDatasetStore
{
    /// <summary>
    /// Adds a dataset to the store.
    /// </summary>
    /// <param name="file">The stream of the dataset file.</param>
    /// <param name="fileName">The name of the dataset file.</param>
    /// <returns>The information about the added dataset.</returns>
    DatasetInfo Add(Stream file, string fileName);
    /// <summary>
    /// Lists all the datasets in the store.
    /// </summary>
    /// <returns>A read-only list of dataset information.</returns>
    IReadOnlyList<DatasetInfo> List();
    /// <summary>
    /// Gets the path of a dataset.
    /// </summary>
    /// <param name="datasetId">The unique identifier of the dataset.</param>
    /// <returns>The path to the dataset file.</returns>
    string GetPath(string datasetId);
}