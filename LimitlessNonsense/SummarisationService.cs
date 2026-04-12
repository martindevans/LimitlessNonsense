using LimitlessNonsense.Cleanup.Actions;

namespace LimitlessNonsense;

/// <summary>
/// Tracks the in-flight summarisation task for a conversation
/// </summary>
public class SummarisationService
{
    private SummarisationTask? _activeTask;

    /// <summary>
    /// Get the currently active summarisation task, or null if none is in progress
    /// </summary>
    public SummarisationTask? GetActiveTask() => _activeTask;

    /// <summary>
    /// Set the active summarisation task (pass null to clear the slot)
    /// </summary>
    public void SetActiveTask(SummarisationTask? task) => _activeTask = task;
}
