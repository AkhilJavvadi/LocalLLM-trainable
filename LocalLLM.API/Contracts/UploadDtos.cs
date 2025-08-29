// This file contains the data transfer objects for uploads.
using Microsoft.AspNetCore.Http;

namespace LocalLLM.Api.Contracts;
/// <summary>
/// Represents a request to upload a dataset.
/// </summary>
public sealed class UploadDatasetRequest
{
    /// <summary>
    /// The file to upload.
    /// </summary>
    public IFormFile File { get; set; } = default!;
}