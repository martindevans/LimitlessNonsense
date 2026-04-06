using System.Text.Json;
using LimitlessNonsense.Cleanup;
using LimitlessNonsense.Cleanup.Actions;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class RemoveRoleTests
{
    private static CleanupContext Context(params ContextMessage[] messages)
        => new(Condition.True(), new ContextState(Guid.NewGuid(), 100, 200), messages.ToList());

    private static ContextMessage Msg(MessageRole role)
        => new ContextMessage(role);

    // -------------------------------------------------------------------------
    // Execute - basic behaviour
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Execute_RemovesMatchingMessages_LeavesOthers()
    {
        var user = Msg(MessageRole.User);
        var assistant = Msg(MessageRole.Assistant);
        var context = Context(user, assistant);

        ContextAction.RemoveRole(MessageRole.User).Execute(context);

        CollectionAssert.DoesNotContain(context.Messages.ToList(), user);
        CollectionAssert.Contains(context.Messages.ToList(), assistant);
    }

    [TestMethod]
    public void Execute_NoMatchingMessages_LeavesAllMessages()
    {
        var user = Msg(MessageRole.User);
        var context = Context(user);

        ContextAction.RemoveRole(MessageRole.Assistant).Execute(context);

        CollectionAssert.Contains(context.Messages.ToList(), user);
    }

    [TestMethod]
    public void Execute_EmptyMessages_DoesNothing()
    {
        var context = Context();

        ContextAction.RemoveRole(MessageRole.User).Execute(context);

        Assert.IsEmpty(context.Messages);
    }

    // -------------------------------------------------------------------------
    // Execute - depth behaviour
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Execute_WithDepth_ProtectsRecentMessages()
    {
        var buried = Msg(MessageRole.User);
        var recent = Msg(MessageRole.User);
        var context = Context(buried, recent);

        // depth=1 protects the last 1 message (recent), but not the older one (buried)
        ContextAction.RemoveRole(MessageRole.User, depth: 1).Execute(context);

        CollectionAssert.DoesNotContain(context.Messages.ToList(), buried);
        CollectionAssert.Contains(context.Messages.ToList(), recent);
    }

    [TestMethod]
    public void Execute_DepthCoversAllMessages_RemovesNothing()
    {
        var msg1 = Msg(MessageRole.User);
        var msg2 = Msg(MessageRole.User);
        var context = Context(msg1, msg2);

        ContextAction.RemoveRole(MessageRole.User, depth: 2).Execute(context);

        Assert.HasCount(2, context.Messages);
    }

    [TestMethod]
    public void Execute_DepthExceedsMessageCount_RemovesNothing()
    {
        var msg = Msg(MessageRole.User);
        var context = Context(msg);

        ContextAction.RemoveRole(MessageRole.User, depth: 100).Execute(context);

        Assert.HasCount(1, context.Messages);
    }

    // -------------------------------------------------------------------------
    // Execute - combined role flags
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Execute_CombinedRoles_RemovesMessagesMatchingAnyRole()
    {
        var reasoning = Msg(MessageRole.Reasoning);
        var tool = Msg(MessageRole.Tool);
        var user = Msg(MessageRole.User);
        var context = Context(reasoning, tool, user);

        ContextAction.RemoveRole(MessageRole.Reasoning | MessageRole.Tool).Execute(context);

        CollectionAssert.DoesNotContain(context.Messages.ToList(), reasoning);
        CollectionAssert.DoesNotContain(context.Messages.ToList(), tool);
        CollectionAssert.Contains(context.Messages.ToList(), user);
    }

    // -------------------------------------------------------------------------
    // JSON serialization
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RemoveRole_SerializesWithCorrectDiscriminator_AndRoundTrips()
    {
        var action = ContextAction.RemoveRole(MessageRole.Reasoning | MessageRole.Tool, depth: 3);

        var json = JsonSerializer.Serialize(action);
        var deserialized = JsonSerializer.Deserialize<ContextAction>(json);

        StringAssert.Contains(json, "\"$type\":\"RemoveRole\"");
        Assert.AreEqual(action, deserialized);
    }
}
