// This file contains the interface for the trainer and the records for the training job and status.
namespace LocalLLM.Api.Services.Train;
/// <summary>
/// Represents the state of a training job.
/// </summary>
public enum TrainState { Pending, Running, Succeeded, Failed, Cancelled }
/// <summary>
/// Represents a request to launch a training job.
/// </summary>
/// <param name="DatasetId">The unique identifier of the dataset.</param>
/// <param name="BaseModel">The base model to use for training.</param>
/// <param name="Epochs">The number of epochs to train for.</param>
/// <param name="LearningRate">The learning rate to use for training.</param>
public record LaunchRequest(string DatasetId, string BaseModel, int Epochs, float LearningRate);
/// <summary>
/// Represents a training job.
/// </summary>
/// <param name="JobId">The unique identifier of the training job.</param>
/// <param name="State">The state of the training job.</param>
/// <param name="StartedAt">The date and time the training job was started.</param>
public record TrainJob(string JobId, TrainState State, DateTime StartedAt);
/// <summary>
/// Represents the interface for a trainer.
/// </summary>
public interface ITrainer
{
    /// <summary>
    /// Launches a training job asynchronously.
    /// </summary>
    /// <param name="request">The request to launch a training job.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the training job.</returns>
    Task<TrainJob> LaunchAsync(LaunchRequest request, CancellationToken ct);
    /// <summary>
    /// Gets the status of a training job.
    /// </summary>
    /// <param name="jobId">The unique identifier of the training job.</param>
    /// <returns>The status of the training job.</returns>
    TrainerStatus Status(string jobId);
}
/// <summary>
/// Represents the status of a training job.
/// </summary>
/// <param name="JobId">The unique identifier of the training job.</param>
/// <param name="State">The state of the training job.</param>
/// <param name="LogsTail">The tail of the logs for the training job.</param>
/// <param name="ArtifactPath">The path to the artifacts for the training job.</param>
public record TrainerStatus(string JobId, TrainState State, string LogsTail, string? ArtifactPath);