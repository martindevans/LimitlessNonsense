using System.Globalization;
using LimitlessNonsense.Middleware.Metadata;

namespace LimitlessNonsense.Middleware.Time;

/// <summary>
/// Insert a message like "The date is now $date" when the date from the previous message to the current one changes.
/// </summary>
public class DateChangedMessage
    : IMiddleware
{
    private readonly string _prefix;
    private readonly string _format;
    private readonly string _suffix;

    public DateChangedMessage(string prefix = "The date is now ", string format = "ddd dd-MMM-yyyy", string suffix = "")
    {
        _prefix = prefix;
        _format = format;
        _suffix = suffix;
    }

    public async Task Process(MiddlewareContext context, Func<MiddlewareContext, Task> next)
    {
        var previous = FindLastDate(context);
        var dateNow = DateOnly.FromDateTime(context.Now);

        // If the date has changed since the last message that was tagged with a date, add the message
        if (previous.HasValue && dateNow != previous.Value)
        {
            var msg = context.AddMessage(
                MessageRole.Tool,
                Importance.Low,
                $"{_prefix}{context.Now.Date.ToString(_format, CultureInfo.InvariantCulture)}{_suffix}"
            );

            // Tag it with a date, so we can be sure this won't happen again
            msg.SetMetadata(new MessageCreationTime(context.Now.Date.AddTicks(1)));
        }

        await next(context);
    }

    private static DateOnly? FindLastDate(MiddlewareContext context)
    {
        for (var i = context.History.Count - 1; i >= 0; i--)
        {
            var msg = context.History[i];
            var creation = msg.TryGetMetadata<MessageCreationTime>();
            if (creation != null)
                return DateOnly.FromDateTime(creation.Time);
        }

        return null;
    }
}