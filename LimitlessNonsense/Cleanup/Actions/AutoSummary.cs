namespace LimitlessNonsense.Cleanup.Actions;

/// <summary>
/// Begin automatically summarising the context, keeping some of the most recent messages at the end.
/// </summary>
/// <param name="Keep"></param>
internal record BeginSummarise(ushort Keep)
    : ContextAction
{
    public override bool Execute(CleanupContext context)
    {
        // Get summary system, if there isn't one early exit
        var provider = (ISummarisationProvider?)context.Services?.GetService(typeof(ISummarisationProvider));
        if (provider == null)
            return false;

        // Check if there's a summarisation already in progress, if so do nothing
        if (context.ActiveSummarisationTask != null)
            return false;

        // Select messages to summarise: all non-system messages except the last `Keep` ones
        var messagesToSummarise = context.Messages
            .Where(m => (m.Role & MessageRole.System) == 0)
            .SkipLast(Keep)
            .ToList();

        if (messagesToSummarise.Count == 0)
            return false;

        // Create transcript from messages
        var transcript = string.Join("\n", messagesToSummarise
            .Select(m => $"{m.Role}: {m.Prefix}{m.Content}{m.Suffix}"));

        // Begin summarisation
        var cts = new CancellationTokenSource();
        var task = provider.Summarise(transcript);

        // Store in-flight summarisation task into slot
        context.ActiveSummarisationTask = new SummarisationTask(
            messagesToSummarise.Select(m => (m.ID, m.Role)).ToList(),
            task,
            cts);

        return true;
    }
}

/// <summary>
/// Wait on a summary task and swap it into the context.
/// </summary>
/// <param name="Block"></param>
internal record EndSummarise(bool Block)
    : ContextAction
{
    public override bool Execute(CleanupContext context)
    {
        // Check slot for in-flight summarisation
        var activeTask = context.ActiveSummarisationTask;
        if (activeTask == null)
            return false;

        // Block if requested
        if (Block)
            activeTask.Task.GetAwaiter().GetResult();

        // If task is not yet complete, nothing to do yet
        if (!activeTask.Task.IsCompleted)
            return false;

        var summary = activeTask.Task.GetAwaiter().GetResult();

        // Check if the original messages still exist
        var messageIds = activeTask.Messages.Select(m => m.ID).ToHashSet();
        var existingMessages = context.Messages
            .Where(m => messageIds.Contains(m.ID))
            .ToList();

        // Clear the slot regardless of whether messages were found
        context.ActiveSummarisationTask = null;

        if (existingMessages.Count == 0)
            return false;

        // Find the position of the first original message to determine where to insert the summary
        var firstIndex = context.Messages.IndexOf(existingMessages[0]);

        // If they do, replace them with the summary
        foreach (var msg in existingMessages)
            context.Messages.Remove(msg);

        var summaryMessage = new ContextMessage(MessageRole.Summary, content: summary);
        context.Messages.Insert(Math.Min(firstIndex, context.Messages.Count), summaryMessage);

        return true;
    }
}

public class SummarisationTask
{
    /// <summary>
    /// The messages which are being summarised
    /// </summary>
    public IReadOnlyList<(Guid ID, MessageRole Role)> Messages { get; }

    /// <summary>
    /// The task that will eventually produce a summary
    /// </summary>
    public Task<string> Task { get; }

    /// <summary>
    /// aCan be used to cancel <see cref="Task"/>
    /// </summary>
    public CancellationTokenSource Cancellation { get; }

    internal SummarisationTask(IReadOnlyList<(Guid ID, MessageRole Role)> messages, Task<string> task, CancellationTokenSource cancellation)
    {
        Messages = messages;
        Task = task;
        Cancellation = cancellation;
    }
}