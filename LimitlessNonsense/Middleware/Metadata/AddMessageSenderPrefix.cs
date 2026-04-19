using LimitlessNonsense.Metadata;

namespace LimitlessNonsense.Middleware.Metadata;

/// <summary>
/// Adds a prefix to message content
/// </summary>
public class AddMessageSenderPrefix
    : IMiddleware
{
    private readonly string _pre;
    private readonly string _post;
    private readonly MessageRole _exclude;

    public AddMessageSenderPrefix(string pre = "[", string post = "]", MessageRole exclude = MessageRole.None)
    {
        _pre = pre;
        _post = post;
        _exclude = exclude;
    }

    public async Task Process(MiddlewareContext context, Func<MiddlewareContext, Task> next)
    {
        if ((context.Message.Role & _exclude) != 0)
            return;

        // Set prefix to sender name, e.g. `[Martin]Content`
        var sender = context.Message.TryGetMetadata<MessageSender>();
        if (!string.IsNullOrWhiteSpace(sender?.Name))
            context.Message.Prefix = $"{_pre}{sender.Name}{_post}{context.Message.Prefix}";

        await next(context);
    }
}