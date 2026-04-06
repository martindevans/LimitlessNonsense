namespace LimitlessNonsense.Middleware;

/// <summary>
/// A pipeline of operations that are applied to messages
/// </summary>
public class Pipeline
{
    private readonly IMiddleware[] _middleware;

    public Pipeline(params Span<IMiddleware> middleware)
    {
        _middleware = middleware.ToArray();
    }

    public void Apply(MiddlewareContext context)
    {
        Func<MiddlewareContext, Task> next = _ => Task.CompletedTask;
        for (var i = _middleware.Length - 1; i >= 0; i--)
        {
            var middleware = _middleware[i];
            
            // Capture the current 'next' delegate
            var currentNext = next;
            
            // Create a new 'next' delegate that calls the middleware's Process method
            next = ctx => middleware.Process(ctx, currentNext);
        }

        // Start the pipeline execution by calling the first middleware's Process method
        next(context);
    }
}