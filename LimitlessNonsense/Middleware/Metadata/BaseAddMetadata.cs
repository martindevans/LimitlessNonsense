namespace LimitlessNonsense.Middleware.Metadata;

/// <summary>
/// Base class for pipeline stages which add metadata
/// </summary>
/// <typeparam name="TMetadata"></typeparam>
/// <param name="overwrite"></param>
public abstract class BaseAddMetadata<TMetadata>(bool overwrite = false)
    : IMiddleware
    where TMetadata : class, IMessageMetadata
{
    protected abstract TMetadata Metadata(MiddlewareContext context);

    public async Task Process(MiddlewareContext context, Func<MiddlewareContext, Task> next)
    {
        // Add the metadata if necessary
        if (overwrite || !context.Message.HasMetadata<TMetadata>())
            context.Message.SetMetadata(Metadata(context));

        await next(context);
    }
}
