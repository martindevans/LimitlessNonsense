using LimitlessNonsense.Metadata;
using System.Globalization;

namespace LimitlessNonsense.Middleware.Metadata;

/// <summary>
/// Adds a prefix to message content with the time of the message was created
/// </summary>
public class AddMessageCreationTimePrefix<TUserData>
    : IMiddleware<TUserData>
{
    private readonly string _format;
    private readonly string _pre;
    private readonly string _post;
    private readonly MessageRole _exclude;

    public AddMessageCreationTimePrefix(string format = "t", string pre = "[", string post = "]", MessageRole exclude = MessageRole.None)
    {
        _format = format;
        _pre = pre;
        _post = post;
        _exclude = exclude;
    }

    public async Task Process(MiddlewareContext<TUserData> context, Func<MiddlewareContext<TUserData>, Task> next)
    {
        if ((context.Message.Role & _exclude) == 0)
        {
            // Set prefix to send time, e.g. `[13:46]Content`
            var time = context.Message.TryGetMetadata<MessageCreationTime>();
            if (time != null)
            {
                var t = time.Time.ToString(_format, CultureInfo.InvariantCulture);
                context.Message.Prefix = $"{_pre}{t}{_post}{context.Message.Prefix}";
            }
        }

        await next(context);
    }
}