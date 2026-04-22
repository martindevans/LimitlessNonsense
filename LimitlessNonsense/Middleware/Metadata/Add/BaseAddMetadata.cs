using LimitlessNonsense.Metadata;

namespace LimitlessNonsense.Middleware.Metadata.Add;

/// <summary>
/// Base class for pipeline stages which add metadata
/// </summary>
/// <typeparam name="TMetadata"></typeparam>
/// <typeparam name="TUserData"></typeparam>
/// <param name="overwrite"></param>
public abstract class BaseAddMetadata<TMetadata, TUserData>(bool overwrite = false)
    : IMiddleware<TUserData>
    where TMetadata : class, IMessageMetadata
{
    protected abstract TMetadata Metadata(MiddlewareContext<TUserData> context);

    public async Task Process(MiddlewareContext<TUserData> context, Func<MiddlewareContext<TUserData>, Task> next)
    {
        // Add the metadata if necessary
        if (overwrite || !context.Message.HasMetadata<TMetadata>())
            context.Message.SetMetadata(Metadata(context));

        await next(context);
    }
}
