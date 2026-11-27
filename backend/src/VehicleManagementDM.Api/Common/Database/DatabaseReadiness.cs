namespace VehicleManagementDM.Api.Common.Database;

/// <summary>
/// Becomes ready once startup migrations have been applied (ARCHITECTURE.md §3). Until then API
/// requests get 503 DATABASE_NOT_READY and error logs go to the fallback file.
/// </summary>
public sealed class DatabaseReadiness
{
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsReady => _ready.Task.IsCompleted;

    public void MarkReady() => _ready.TrySetResult();

    /// <summary>Waits until the database is ready (used by background jobs and tests).</summary>
    public Task WaitAsync(CancellationToken cancellationToken) => _ready.Task.WaitAsync(cancellationToken);
}
