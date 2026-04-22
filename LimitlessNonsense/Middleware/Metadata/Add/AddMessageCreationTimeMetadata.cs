using LimitlessNonsense.Metadata;

namespace LimitlessNonsense.Middleware.Metadata.Add;

/// <summary>
/// Add UtcNow as the <see cref="MessageCreationTime"/> metadata on this message
/// </summary>
/// <param name="overwrite"></param>
public class AddMessageCreationTimeMetadata<TUserData>(bool overwrite = false)
    : BaseAddMetadata<MessageCreationTime, TUserData>(overwrite)
{
    protected override MessageCreationTime Metadata(MiddlewareContext<TUserData> context)
    {
        return new MessageCreationTime(context.UtcNow);
    }
}
