using LimitlessNonsense.Metadata;

namespace LimitlessNonsense.Middleware.Metadata.Add;

/// <summary>
/// Add UtcNow as the <see cref="MessageCreationTime"/> metadata on this message
/// </summary>
/// <param name="overwrite"></param>
public class AddMessageCreationTimeMetadata(bool overwrite = false)
    : BaseAddMetadata<MessageCreationTime>(overwrite)
{
    protected override MessageCreationTime Metadata(MiddlewareContext context)
    {
        return new MessageCreationTime(context.UtcNow);
    }
}
