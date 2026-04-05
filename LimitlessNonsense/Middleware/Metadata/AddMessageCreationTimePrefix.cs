using LimitlessNonsense.Metadata;
using System.Globalization;

namespace LimitlessNonsense.Middleware.Metadata;

/// <summary>
/// Adds a prefix to message content with the time of the message was created
/// </summary>
public class AddMessageCreationTimePrefix
    : IMiddleware
{
    private readonly string _format;
    private readonly string _pre;
    private readonly string _post;

    public AddMessageCreationTimePrefix(string format = "t", string pre = "[", string post = "]")
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