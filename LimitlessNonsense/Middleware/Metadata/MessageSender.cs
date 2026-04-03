namespace LimitlessNonsense.Middleware.Metadata;

/// <summary>
/// Adds a prefix to message content
/// </summary>
public class AddMessageSenderPrefix
    : IMiddleware
{
    private readonly string _pre;
    private readonly string _post;

    public AddMessageSenderPrefix(string pre = "[", string post = "]")
    {
        _pre = pre;
        _post = post;
    }

    public async Task Process(MiddlewareContext context, Func<MiddlewareContext, Task> next)
    {
        // Set prefix to sender name, e.g. `[Martin]Content`
        var sender = context.Message.TryGetMetadata<MessageSender>();
        if (!string.IsNullOrWhiteSpace(sender?.Name))
            context.Message.Prefix = $"{_pre}{sender.Name}{_post}{context.Message.Prefix}";

        await next(context);
    }
}

/// <summary>
/// Metadata indicating the name of the sender of this message
/// </summary>
/// <param name="Name"></param>
public record MessageSender(string Name)
    : IMessageMetadata;