namespace LimitlessNonsense.Middleware;

/// <summary>
/// A pipeline of operations that are applied to messages
/// </summary>
public class Pipeline<TUserData>
{
    private readonly Func<MiddlewareContext<TUserData>, Task> _run;

    public Pipeline(params Span<IMiddleware<TUserData>> middleware)
    {
        _run = _ => Task.CompletedTask;
        for (var i = middleware.Length - 1; i >= 0; i--)
        {
            var item = middleware[i];

            // Capture the current 'next' delegate
            var currentNext = _run;

            // Create a new 'next' delegate that calls the middleware's Process method
            _run = ctx => item.Process(ctx, currentNext);
        }
    }

    /// <summary>
    /// Apply this pipeline to a context
    /// </summary>
    /// <param name="context"></param>
    public async Task Apply(MiddlewareContext<TUserData> context)
    {
        await _run(context);
    }
}