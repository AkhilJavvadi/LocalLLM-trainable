// This file contains the DatasetController class, which is responsible for handling dataset requests.
using LocalLLM.Api.Contracts;
using LocalLLM.Api.Services.Data;
using Microsoft.AspNetCore.Mvc;

namespace LocalLLM.Api.Controllers;
/// <summary>
/// The DatasetController class, which is responsible for handling dataset requests.
/// </summary>
[ApiController]
[Route("api/dataset")]
public sealed class DatasetController : ControllerBase
{
    private readonly IDatasetStore _store;
    /// <summary>
    /// Initializes a new instance of the <see cref="DatasetController"/> class.
    /// </summary>
    /// <param name="store">The dataset store.</param>
    public DatasetController(IDatasetStore store) => _store = store;
    /// <summary>
    /// Uploads a dataset.
    /// </summary>
    /// <param name="req">The dataset upload request.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the dataset upload response.</returns>
    [HttpPost]
    [RequestSizeLimit(1024L * 1024 * 512)] // 512MB
    [Consumes("multipart/form-data")]       // <-- tell Swagger this is multipart
    public async Task<ActionResult<DatasetUploadResponse>> Upload([FromForm] UploadDatasetRequest req)
    {
        var file = req.File;
        if (file is null || file.Length == 0) return BadRequest("No file uploaded.");

        await using var stream = file.OpenReadStream();
        var info = _store.Add(stream, file.FileName);
        return Ok(new DatasetUploadResponse(info.Id, info.Path, info.Count, info.CreatedAt));
    }
    /// <summary>
    /// Lists all datasets.
    /// </summary>
    /// <returns>A list of datasets.</returns>
    [HttpGet]
    public ActionResult<IEnumerable<DatasetListItem>> List()
        => Ok(_store.List().Select(x => new DatasetListItem(x.Id, x.Path, x.Count, x.CreatedAt)));
}