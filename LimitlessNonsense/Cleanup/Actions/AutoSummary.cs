namespace LimitlessNonsense.Cleanup.Actions;

/// <summary>
/// Begin automatically summarising the context, keeping some of the most recent messages at the end.
/// </summary>
/// <param name="KeepStart"></param>
/// <param name="KeepEnd"></param>
/// <param name="PreserveSystemStart">Skip `System` role messages at the start</param>
/// <param name="DeleteRoles">Messages with this role in the range will be ignored in the summary and deleted when the summary is swapped in</param>
internal record BeginSummarise(ushort KeepStart, ushort KeepEnd, bool PreserveSystemStart, MessageRole DeleteRoles)
    : ContextAction
{
    public override async Task<bool> Execute(CleanupContext context)
    {
        // Early exit if there's already an in-flight summary task
        if (context.ActiveSummarisationTask != null)
            return false;

        // Get summary system
        var provider = (ISummarisationProvider?)context.Services?.GetService(typeof(ISummarisationProvider));
        if (provider == null)
            return false;

        // Select all messages in the range defined by keep start/end
        var messagesInRange = context.Messages
            .Skip(KeepStart)
            .SkipLast(KeepEnd)
            .ToList();

        // Remove system role messages at the start
        if (PreserveSystemStart)
            while (messagesInRange.Count > 0 && messagesInRange[0].Role == MessageRole.System)
                messagesInRange.RemoveAt(0);

        // Early exit if there's no work to do
        if (messagesInRange.Count == 0)
            return false;
        
        // Create transcript of relevant messages
        var transcript = string.Join("\n",
            messagesInRange
               .Where(m => (m.Role & DeleteRoles) == 0)
               .Select(m => $"{m.Prefix}{m.Content}{m.Suffix}")
        );

        // Begin summarisation
        var cts = new CancellationTokenSource();
        var task = provider.Summarise(transcript, cts.Token);

        // Store in-flight summarisation task into slot
        context.ActiveSummarisationTask = new SummarisationTask(
            messagesInRange.Select(static m => m.ID).ToList(),
            task,
            cts
        );

        // We didn't change the context, so return false
        return false;
    }
}

/// <summary>
/// Wait on a summary task and swap it into the context.
/// </summary>
/// <param name="Block"></param>
internal record EndSummarise(bool Block)
    : ContextAction
{
    public override async Task<bool> Execute(CleanupContext context)
    {
        // Check slot for in-flight summarisation
        var summaryTask = context.ActiveSummarisationTask;
        if (summaryTask == null)
            return false;

        // Early exit if task is still in flight
        if (!summaryTask.Task.IsCompleted && !Block)
            return false;
        
        // Clear the slot, we're about to consume this task
        context.ActiveSummarisationTask = null;

        // Wait for completion
        string summary;
        try
        {
            summary = await summaryTask.Task;
        }
        catch (TaskCanceledException)
        {
            return false;
        }

        // Check if the original messages still exist
        var messageIds = summaryTask.Messages.ToHashSet();
        var existingMessages = context.Messages
            .Where(m => messageIds.Contains(m.ID))
            .ToList();

        // Check that the message that were included in the summary still exist
        if (existingMessages.Count == 0)
            return false;

        // Find the position of the first original message
        var firstIndex = context.Messages.IndexOf(existingMessages[0]);

        // Remove all messages that were included in summary
        foreach (var msg in existingMessages)
            context.Messages.Remove(msg);
        
        // Insert summary
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
    public IReadOnlyList<Guid> Messages { get; }

    /// <summary>
    /// The task that will eventually produce a summary
    /// </summary>
    public Task<string> Task { get; }

    /// <summary>
    /// aCan be used to cancel <see cref="Task"/>
    /// </summary>
    public CancellationTokenSource Cancellation { get; }

    internal SummarisationTask(IReadOnlyList<Guid> messages, Task<string> task, CancellationTokenSource cancellation)
    {
        Messages = messages;
        Task = task;
        Cancellation = cancellation;
    }
}