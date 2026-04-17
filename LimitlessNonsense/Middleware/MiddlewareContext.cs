namespace LimitlessNonsense.Middleware;

public sealed class MiddlewareContext
{
    public MiddlewareContext(List<Message> history, DateTime now, Message message)
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
    public List<Message> History { get; }

    /// <summary>
    /// The new message that is about to be added
    /// </summary>
    public Message Message { get; }
}