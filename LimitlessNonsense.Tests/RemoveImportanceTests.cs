using System.Text.Json;
using LimitlessNonsense.Cleanup;
using LimitlessNonsense.Cleanup.Actions;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class RemoveImportanceTests
{
    private static Message Msg(MessageImportance importance)
        => new Message(MessageRole.User) { Importance = importance };

    private static CleanupContext Context(params Message[] messages)
        => new(Condition.True(), new ContextState(Guid.NewGuid(), 0, 100), messages.ToList());

    // -------------------------------------------------------------------------
    // Basic removal
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Execute_RemovesMessagesAtOrBelowThreshold()
    {
        // Also exercises ContextAction.ImportanceRemoval factory method
        var veryHigh = Msg(MessageImportance.VeryHigh);
        var high     = Msg(MessageImportance.High);
        var normal   = Msg(MessageImportance.Normal);
        var low      = Msg(MessageImportance.Low);
        var veryLow  = Msg(MessageImportance.VeryLow);

        var action = ContextAction.ImportanceRemoval(MessageImportance.Normal);
        var ctx = Context(veryHigh, high, normal, low, veryLow);
        var changed = await action.Execute(ctx);

        // Normal, Low, VeryLow are at or below the threshold; VeryHigh and High remain
        Assert.IsTrue(changed);
        Assert.HasCount(2, ctx.Messages);
        Assert.Contains(veryHigh, ctx.Messages);
        Assert.Contains(high, ctx.Messages);
    }

    // -------------------------------------------------------------------------
    // Depth
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Execute_Depth_ProtectsRecentMessages()
    {
        var old     = Msg(MessageImportance.VeryLow); // index 0 — buried, should be removed
        var recent1 = Msg(MessageImportance.VeryLow); // index 1 — within depth, protected
        var recent2 = Msg(MessageImportance.VeryLow); // index 2 — within depth, protected

        var action = ContextAction.ImportanceRemoval(MessageImportance.Normal, depth: 2);
        var ctx = Context(old, recent1, recent2);
        var changed = await action.Execute(ctx);

        Assert.IsTrue(changed);
        Assert.HasCount(2, ctx.Messages);
        Assert.Contains(recent1, ctx.Messages);
        Assert.Contains(recent2, ctx.Messages);
    }

    // -------------------------------------------------------------------------
    // Edge cases
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Execute_EmptyMessages_DoesNotThrow()
    {
        var action = ContextAction.ImportanceRemoval(MessageImportance.Normal);
        var ctx = Context();

        var changed = await action.Execute(ctx);

        Assert.IsFalse(changed);
        Assert.IsEmpty(ctx.Messages);
    }

    [TestMethod]
    public async Task Execute_DepthExceedsCount_RemovesNothing()
    {
        var msg1 = Msg(MessageImportance.VeryLow);
        var msg2 = Msg(MessageImportance.VeryLow);

        var action = ContextAction.ImportanceRemoval(MessageImportance.Normal, depth: 5);
        var ctx = Context(msg1, msg2);
        var changed = await action.Execute(ctx);

        Assert.IsFalse(changed);
        Assert.HasCount(2, ctx.Messages);
    }

    // -------------------------------------------------------------------------
    // JSON serialization
    // -------------------------------------------------------------------------

    [TestMethod]
    public void ImportanceRemoval_RoundTrips_ThroughJson()
    {
        var action = ContextAction.ImportanceRemoval(MessageImportance.Low, depth: 3);

        var json = JsonSerializer.Serialize(action);
        var deserialized = JsonSerializer.Deserialize<ContextAction>(json);

        Assert.AreEqual(action, deserialized);
    }
}
