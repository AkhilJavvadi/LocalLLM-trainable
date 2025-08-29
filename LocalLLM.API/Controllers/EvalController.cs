// This file contains the EvalController class, which is responsible for handling evaluation requests.
using Microsoft.AspNetCore.Mvc;

namespace LocalLLM.Api.Controllers;
/// <summary>
/// The EvalController class, which is responsible for handling evaluation requests.
/// </summary>
[ApiController]
[Route("api/eval")]
public sealed class EvalController : ControllerBase
{
    /// <summary>
    /// Performs a quick evaluation of a model.
    /// </summary>
    /// <param name="model">The model to evaluate.</param>
    /// <param name="datasetId">The ID of the dataset to use for a quick evaluation.</param>
    /// <returns>An object containing the evaluation results.</returns>
    [HttpPost("{model}/quick")]
    public ActionResult<object> QuickEval(string model, [FromQuery] string datasetId)
    {
        // TODO: implement a small prompt/answer loop and compute simple exact-match/ROUGE
        return Ok(new { Model = model, DatasetId = datasetId, Score = 0.0, Items = Array.Empty<object>() });
    }
}