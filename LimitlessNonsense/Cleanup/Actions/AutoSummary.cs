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
        var provider = (ISummarisationProvider?)context.Services.GetService(typeof(ISummarisationProvider));
        if (provider == null)
            return false;
        
        // todo: Check if there's a summarisation already in progress, if so do nothing
        
        // todo: Select messages to summarise
        // todo: Create transcript from messages
        // todo: Begin summarisation
        // todo: Store in-flight summarisation task into slot
        
        throw new NotImplementedException();
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
        //todo: Check slot for in-flight summarisation
        //todo: Block if requested
        //todo: Check if the original messages still exist
        //todo: If they do, replace them with the summary
        
        throw new NotImplementedException();
    }
}

public class SummarisationTask
{
    /// <summary>
    /// The messages which are being summarised
    /// </summary>
    public IReadOnlyList<(Guid, MessageRole)> Messages { get; }

    /// <summary>
    /// The task that will eventually produce a summary
    /// </summary>
    public Task<string> Task { get; }

    /// <summary>
    /// aCan be used to cancel <see cref="Task"/>
    /// </summary>
    public CancellationTokenSource Cancellation { get; }

    internal SummarisationTask(IReadOnlyList<(Guid, MessageRole)> messages, Task<string> task, CancellationTokenSource cancellation)
    {
        Messages = messages;
        Task = task;
        Cancellation = cancellation;
    }
}