// This file contains the data transfer objects for datasets.
namespace LocalLLM.Api.Contracts;
/// <summary>
/// Represents a response to a dataset upload request.
/// </summary>
/// <param name="Id">The unique identifier of the dataset.</param>
/// <param name="Path">The path to the dataset file.</param>
/// <param name="Count">The number of records in the dataset.</param>
/// <param name="CreatedAt">The date and time the dataset was created.</param>
public record DatasetUploadResponse(string Id, string Path, int Count, DateTime CreatedAt);
/// <summary>
/// Represents a dataset list item.
/// </summary>
/// <param name="Id">The unique identifier of the dataset.</param>
/// <param name="Path">The path to the dataset file.</param>
/// <param name="Count">The number of records in the dataset.</param>
/// <param name="CreatedAt">The date and time the dataset was created.</param>
public record DatasetListItem(string Id, string Path, int Count, DateTime CreatedAt);