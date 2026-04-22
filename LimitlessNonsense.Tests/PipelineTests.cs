using LimitlessNonsense.Middleware;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class PipelineTests
{
    private static MiddlewareContext<int> Context()
        => new([], DateTime.UtcNow, new Message(MessageRole.User), 0);

    // Empty pipeline
    [TestMethod]
    public void Apply_EmptyPipeline_DoesNotThrow()
    {
        var pipeline = new Pipeline<int>();
        var context = Context();

        pipeline.Apply(context);
    }

    // Single middleware
    [TestMethod]
    public void Apply_SingleMiddleware_IsInvoked()
    {
        var called = false;
        var middleware = new FuncMiddleware<int>((ctx, next) => { called = true; return next(ctx); });
        var pipeline = new Pipeline<int>(middleware);

        pipeline.Apply(Context());

        Assert.IsTrue(called);
    }

    [TestMethod]
    public void Apply_SingleMiddleware_ReceivesCorrectContext()
    {
        MiddlewareContext<int>? received = null;
        var middleware = new FuncMiddleware<int>((ctx, next) => { received = ctx; return next(ctx); });
        var pipeline = new Pipeline<int>(middleware);
        var context = Context();

        pipeline.Apply(context);

        Assert.AreSame(context, received);
    }

    // Multiple middleware – execution order
    [TestMethod]
    public void Apply_MultipleMiddleware_AreCalledInOrder()
    {
        var order = new List<int>();
        var m1 = new FuncMiddleware<int>((ctx, next) => { order.Add(1); return next(ctx); });
        var m2 = new FuncMiddleware<int>((ctx, next) => { order.Add(2); return next(ctx); });
        var m3 = new FuncMiddleware<int>((ctx, next) => { order.Add(3); return next(ctx); });
        var pipeline = new Pipeline<int>(m1, m2, m3);

        pipeline.Apply(Context());

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, order);
    }

    // Same context flows through the whole chain
    [TestMethod]
    public void Apply_MultipleMiddleware_SameContextPassedToAll()
    {
        var contexts = new List<MiddlewareContext<int>>();
        var m1 = new FuncMiddleware<int>((ctx, next) => { contexts.Add(ctx); return next(ctx); });
        var m2 = new FuncMiddleware<int>((ctx, next) => { contexts.Add(ctx); return next(ctx); });
        var pipeline = new Pipeline<int>(m1, m2);
        var context = Context();

        pipeline.Apply(context);

        Assert.HasCount(2, contexts);
        Assert.AreSame(context, contexts[0]);
        Assert.AreSame(context, contexts[1]);
    }

    // Short-circuit: middleware that does not call next stops the chain
    [TestMethod]
    public void Apply_MiddlewareShortCircuits_SubsequentMiddlewareNotCalled()
    {
        var secondCalled = false;
        var m1 = new FuncMiddleware<int>((ctx, next) => Task.CompletedTask); // does not call next
        var m2 = new FuncMiddleware<int>((ctx, next) => { secondCalled = true; return next(ctx); });
        var pipeline = new Pipeline<int>(m1, m2);

        pipeline.Apply(Context());

        Assert.IsFalse(secondCalled);
    }

    // Middleware can execute logic after calling next (synchronous continuation)
    [TestMethod]
    public void Apply_MiddlewarePostProcessing_RunsAfterDownstreamMiddleware()
    {
        var order = new List<string>();
        var m1 = new FuncMiddleware<int>((ctx, next) =>
        {
            order.Add("before");
            var task = next(ctx);
            order.Add("after");
            return task;
        });
        var m2 = new FuncMiddleware<int>((ctx, next) => { order.Add("inner"); return next(ctx); });
        var pipeline = new Pipeline<int>(m1, m2);

        pipeline.Apply(Context());

        CollectionAssert.AreEqual(new[] { "before", "inner", "after" }, order);
    }
}
