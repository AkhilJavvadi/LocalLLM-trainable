// This file contains the data transfer objects for training.
namespace LocalLLM.Api.Contracts;
/// <summary>
/// Represents a request to launch a training job.
/// </summary>
/// <param name="DatasetId">The unique identifier of the dataset.</param>
/// <param name="BaseModel">The base model to use for training.</param>
/// <param name="Epochs">The number of epochs to train for.</param>
/// <param name="LearningRate">The learning rate to use for training.</param>
public record TrainLaunchRequest(string DatasetId, string? BaseModel, int? Epochs, float? LearningRate);
/// <summary>
/// Represents a response to a training launch request.
/// </summary>
/// <param name="JobId">The unique identifier of the training job.</param>
/// <param name="State">The state of the training job.</param>
/// <param name="StartedAt">The date and time the training job was started.</param>
public record TrainLaunchResponse(string JobId, string State, DateTime StartedAt);
/// <summary>
/// Represents a response to a training status request.
/// </summary>
/// <param name="JobId">The unique identifier of the training job.</param>
/// <param name="State">The state of the training job.</param>
/// <param name="LogsTail">The tail of the logs for the training job.</param>
/// <param name="ArtifactPath">The path to the artifacts for the training job.</param>
public record TrainStatusResponse(string JobId, string State, string LogsTail, string? ArtifactPath);