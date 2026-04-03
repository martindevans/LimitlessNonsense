using System.Globalization;

namespace LimitlessNonsense.Middleware.Metadata;

/// <summary>
/// Add UtcNow as the <see cref="MessageCreationTime"/> metadata on this message
/// </summary>
/// <param name="overwrite"></param>
public class AddMessageCreationTimeMetadata(bool overwrite = false)
    : BaseAddMetadata<MessageCreationTime>(overwrite)
{
    protected override MessageCreationTime Metadata(MiddlewareContext context)
    {
        return new MessageCreationTime(context.Now);
    }
}

/// <summary>
/// Adds a prefix to message content
/// </summary>
public class AddMessageTimePrefix
    : IMiddleware
{
    private readonly string _format;
    private readonly string _pre;
    private readonly string _post;

    public AddMessageTimePrefix(string format = "t", string pre = "[", string post = "]")
    {
        _format = format;
        _pre = pre;
        _post = post;
    }

    public async Task Process(MiddlewareContext context, Func<MiddlewareContext, Task> next)
    {
        // Set prefix to send time, e.g. `[13:46]Content`
        var time = context.Message.TryGetMetadata<MessageCreationTime>();
        if (time != null)
        {
            var t = time.Time.ToString(_format, CultureInfo.InvariantCulture);
            context.Message.Prefix = $"{_pre}{t}{_post}{context.Message.Prefix}";
        }

        await next(context);
    }
}

/// <summary>
/// Metadata indicating when this message was created
/// </summary>
/// <param name="Time"></param>
public sealed record MessageCreationTime(DateTime Time)
    : IMessageMetadata;