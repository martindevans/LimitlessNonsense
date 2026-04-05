namespace LimitlessNonsense.Middleware;

public interface IMiddleware
{
    public Task Process(MiddlewareContext context, Func<MiddlewareContext, Task> next);
}

// Middleware:
// - Modify new message
// - Emit another message before or after
// - Modify any of past messages

public sealed class MiddlewareContext
{
    internal MiddlewareContext(List<ContextMessage> history, DateTime now, ContextMessage message)
    {
        History = history;
        Now = now;
        Message = message;
    }

    public DateTime Now { get; }

    #region history
    /// <summary>
    /// History of all messages
    /// </summary>
    public List<ContextMessage> History { get; }

    /// <summary>
    /// The new message that is about to be added
    /// </summary>
    public ContextMessage Message { get; }
    #endregion
}