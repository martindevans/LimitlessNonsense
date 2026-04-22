namespace LimitlessNonsense.Middleware;

public interface IMiddleware<TUserData>
{
    public Task Process(MiddlewareContext<TUserData> context, Func<MiddlewareContext<TUserData>, Task> next);
}

/// <summary>
/// Wraps a func as middleware
/// </summary>
/// <param name="func"></param>
public class FuncMiddleware<TUserData>(Func<MiddlewareContext<TUserData>, Func<MiddlewareContext<TUserData>, Task>, Task> func)
    : IMiddleware<TUserData>
{
    public async Task Process(MiddlewareContext<TUserData> context, Func<MiddlewareContext<TUserData>, Task> next)
    {
        await func(context, next);
    }
}