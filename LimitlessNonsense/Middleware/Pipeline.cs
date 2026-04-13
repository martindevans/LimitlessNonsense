namespace LimitlessNonsense.Middleware;

/// <summary>
/// A pipeline of operations that are applied to messages
/// </summary>
public class Pipeline
{
    private readonly Func<MiddlewareContext, Task> _run;

    public Pipeline(params Span<IMiddleware> middleware)
    {
        var middleware1 = middleware.ToArray();

        _run = _ => Task.CompletedTask;
        for (var i = middleware1.Length - 1; i >= 0; i--)
        {
            var item = middleware1[i];

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
    public void Apply(MiddlewareContext context)
    {
        _run(context);
    }
}