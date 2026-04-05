namespace LimitlessNonsense.Middleware;

public interface IMiddleware
{
    public Task Process(MiddlewareContext context, Func<MiddlewareContext, Task> next);
}