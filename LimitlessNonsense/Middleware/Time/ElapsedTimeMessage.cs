using System.Globalization;
using Humanizer;
using LimitlessNonsense.Middleware.Metadata;

namespace LimitlessNonsense.Middleware.Time;

/// <summary>
/// Adds an ephemeral message indicating the elapsed time since the last message, if it is more than a certain duration
/// </summary>
public class ElapsedTimeMessage
    : IMiddleware
{
    private readonly TimeSpan _duration;

    public ElapsedTimeMessage(TimeSpan duration)
    {
        _duration = duration;
    }

    public async Task Process(MiddlewareContext context, Func<MiddlewareContext, Task> next)
    {
        // Remove previous time message if it exists
        Cleanup(context);

        if (context.History.Count > 0)
        {
            var time = context.History[^1].TryGetMetadata<MessageCreationTime>();
            if (time != null)
            {
                var elapsed = context.Now - time.Time;
                if (elapsed > _duration)
                {
                    // Add new time message
                    var msg = context.AddMessage(
                        MessageRole.Tool,
                        Importance.Ephemeral,
                        content: $"{elapsed.Humanize(culture: CultureInfo.InvariantCulture)} since last message"
                    );

                    // Add metadata marker so we can find it later
                    msg.SetMetadata(new EphemeralTimeElapsedMessage());
                }
            }
        }

        await next(context);
    }

    private static void Cleanup(MiddlewareContext context)
    {
        for (var i = context.History.Count - 1; i >= 0; i--)
        {
            var msg = context.History[i];
            if (msg.HasMetadata<EphemeralTimeElapsedMessage>())
                context.Remove(msg);
        }
    }

    private sealed record EphemeralTimeElapsedMessage
        : IMessageMetadata;
}