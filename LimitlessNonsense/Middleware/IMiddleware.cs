namespace LimitlessNonsense.Middleware;

public interface IMiddleware
{
    public Task Process(MiddlewareContext context, Func<MiddlewareContext, Task> next);
}

// Middleware:
// - Modify new message
// - Emit another message before or after
// - Modify any of past messages

public class MiddlewareContext
{
    internal MiddlewareContext(IReadOnlyList<IContextMessage> history, DateTime now, IContextMessage message)
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
    public IReadOnlyList<IContextMessage> History { get; }

    /// <summary>
    /// The new message that is about to be added
    /// </summary>
    public IContextMessage Message { get; }
    #endregion

    public IContextMessage AddMessage(MessageRole role, Importance importance, string content)
    {
        throw new NotImplementedException();
    }

    public void Remove(IContextMessage msg)
    {
        throw new NotImplementedException();
    }
}