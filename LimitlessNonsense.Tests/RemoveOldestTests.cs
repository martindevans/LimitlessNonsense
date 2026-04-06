using LimitlessNonsense.Cleanup;
using LimitlessNonsense.Cleanup.Actions;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class RemoveOldestTests
{
    private static CleanupContext Context(params ContextMessage[] messages)
        => new(Condition.True(), new ContextState(Guid.NewGuid(), 50, 100), messages.ToList());

    private static ContextMessage Message(MessageRole role)
        => new ContextMessage(role);

    // -------------------------------------------------------------------------
    // Basic Behavior
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Execute_RemovesOldestMatchingMessage()
    {
        var system = Message(MessageRole.System);
        var user = Message(MessageRole.User);
        var assistant = Message(MessageRole.Assistant);
        var ctx = Context(system, user, assistant);

        ContextAction.RemoveOldest(MessageRole.User).Execute(ctx);

        Assert.HasCount(2, ctx.Messages);
        Assert.DoesNotContain(user, ctx.Messages);
        Assert.Contains(system, ctx.Messages);
        Assert.Contains(assistant, ctx.Messages);
    }

    [TestMethod]
    public void Execute_MultipleMatchingMessages_OnlyRemovesOldest()
    {
        var user1 = Message(MessageRole.User);
        var assistant = Message(MessageRole.Assistant);
        var user2 = Message(MessageRole.User);
        var ctx = Context(user1, assistant, user2);

        ContextAction.RemoveOldest(MessageRole.User).Execute(ctx);

        Assert.HasCount(2, ctx.Messages);
        Assert.DoesNotContain(user1, ctx.Messages);
        Assert.Contains(assistant, ctx.Messages);
        Assert.Contains(user2, ctx.Messages);
    }

    [TestMethod]
    public void Execute_CombinedRoles_RemovesOldestMatchingAnyRole()
    {
        var system = Message(MessageRole.System);
        var user = Message(MessageRole.User);
        var assistant = Message(MessageRole.Assistant);
        var ctx = Context(system, user, assistant);

        ContextAction.RemoveOldest(MessageRole.User | MessageRole.Assistant).Execute(ctx);

        Assert.HasCount(2, ctx.Messages);
        Assert.DoesNotContain(user, ctx.Messages);
        Assert.Contains(system, ctx.Messages);
        Assert.Contains(assistant, ctx.Messages);
    }

    // -------------------------------------------------------------------------
    // Edge Cases
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Execute_EmptyContext_DoesNothing()
    {
        var ctx = Context();

        ContextAction.RemoveOldest(MessageRole.User).Execute(ctx);

        Assert.IsEmpty(ctx.Messages);
    }

    [TestMethod]
    public void Execute_NoMatchingRole_DoesNothing()
    {
        var system = Message(MessageRole.System);
        var assistant = Message(MessageRole.Assistant);
        var ctx = Context(system, assistant);

        ContextAction.RemoveOldest(MessageRole.User).Execute(ctx);

        Assert.HasCount(2, ctx.Messages);
        Assert.Contains(system, ctx.Messages);
        Assert.Contains(assistant, ctx.Messages);
    }
}
