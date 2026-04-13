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
    public async Task Execute_RemovesOldestMatchingMessage()
    {
        var system = Message(MessageRole.System);
        var user = Message(MessageRole.User);
        var assistant = Message(MessageRole.Assistant);
        var ctx = Context(system, user, assistant);

        var changed = await ContextAction.RemoveOldest(MessageRole.User).Execute(ctx);

        Assert.IsTrue(changed);
        Assert.HasCount(2, ctx.Messages);
        Assert.DoesNotContain(user, ctx.Messages);
        Assert.Contains(system, ctx.Messages);
        Assert.Contains(assistant, ctx.Messages);
    }

    [TestMethod]
    public async Task Execute_MultipleMatchingMessages_OnlyRemovesOldest()
    {
        var user1 = Message(MessageRole.User);
        var assistant = Message(MessageRole.Assistant);
        var user2 = Message(MessageRole.User);
        var ctx = Context(user1, assistant, user2);

        var changed = await ContextAction.RemoveOldest(MessageRole.User).Execute(ctx);

        Assert.IsTrue(changed);
        Assert.HasCount(2, ctx.Messages);
        Assert.DoesNotContain(user1, ctx.Messages);
        Assert.Contains(assistant, ctx.Messages);
        Assert.Contains(user2, ctx.Messages);
    }

    [TestMethod]
    public async Task Execute_CombinedRoles_RemovesOldestMatchingAnyRole()
    {
        var system = Message(MessageRole.System);
        var user = Message(MessageRole.User);
        var assistant = Message(MessageRole.Assistant);
        var ctx = Context(system, user, assistant);

        var changed = await ContextAction.RemoveOldest(MessageRole.User | MessageRole.Assistant).Execute(ctx);

        Assert.IsTrue(changed);
        Assert.HasCount(2, ctx.Messages);
        Assert.DoesNotContain(user, ctx.Messages);
        Assert.Contains(system, ctx.Messages);
        Assert.Contains(assistant, ctx.Messages);
    }

    // -------------------------------------------------------------------------
    // Edge Cases
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Execute_EmptyContext_DoesNothing()
    {
        var ctx = Context();

        var changed = await ContextAction.RemoveOldest(MessageRole.User).Execute(ctx);

        Assert.IsFalse(changed);
        Assert.IsEmpty(ctx.Messages);
    }

    [TestMethod]
    public async Task Execute_NoMatchingRole_DoesNothing()
    {
        var system = Message(MessageRole.System);
        var assistant = Message(MessageRole.Assistant);
        var ctx = Context(system, assistant);

        var changed = await ContextAction.RemoveOldest(MessageRole.User).Execute(ctx);

        Assert.IsFalse(changed);
        Assert.HasCount(2, ctx.Messages);
        Assert.Contains(system, ctx.Messages);
        Assert.Contains(assistant, ctx.Messages);
    }
}
