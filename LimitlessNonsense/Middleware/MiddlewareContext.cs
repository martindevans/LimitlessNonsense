namespace LimitlessNonsense.Middleware;

public sealed class MiddlewareContext
{
    public MiddlewareContext(List<ContextMessage> history, DateTime now, ContextMessage message)
    {
        History = history;
        UtcNow = now;
        Message = message;
    }

    /// <summary>
    /// The current time
    /// </summary>
    public DateTime UtcNow { get; }

    /// <summary>
    /// History of all messages
    /// </summary>
    public List<ContextMessage> History { get; }

    /// <summary>
    /// The new message that is about to be added
    /// </summary>
    public ContextMessage Message { get; }
}