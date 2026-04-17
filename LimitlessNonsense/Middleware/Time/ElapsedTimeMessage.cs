using System.Globalization;
using Humanizer;
using LimitlessNonsense.Metadata;

namespace LimitlessNonsense.Middleware.Time;

/// <summary>
/// Adds an ephemeral message indicating the elapsed time since the last message, if it is more than a certain threshold
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
        context.History.RemoveAll(static a => a.HasMetadata<EphemeralTimeElapsedMessage>());

        if (context.History.Count > 0)
        {
            var time = context.History[^1].TryGetMetadata<MessageCreationTime>();
            if (time != null)
            {
                var elapsed = context.UtcNow - time.Time;
                if (elapsed > _duration)
                {
                    // Add new time message
                    var msg = new Message(
                        MessageRole.Tool,
                        MessageImportance.Ephemeral,
                        content: $"{elapsed.Humanize(culture: CultureInfo.InvariantCulture)} since last message"
                    );
                    context.History.Add(msg);

                    // Add metadata marker so we can find it later
                    msg.SetMetadata(new EphemeralTimeElapsedMessage());
                }
            }
        }

        await next(context);
    }

    private sealed record EphemeralTimeElapsedMessage
        : IMessageMetadata;
}