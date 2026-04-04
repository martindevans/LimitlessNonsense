using System.Text.Json;
using LimitlessNonsense.Cleanup;
using LimitlessNonsense.Cleanup.Actions;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class RemoveImportanceTests
{
    private static ContextMessage Msg(Importance importance)
        => new ContextMessage(MessageRole.User) { Importance = importance };

    private static CleanupContext Context(params ContextMessage[] messages)
        => new(Condition.True(), new ContextState(Guid.NewGuid(), 0, 100), messages);

    // -------------------------------------------------------------------------
    // Basic removal
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Execute_RemovesMessagesAtOrBelowThreshold()
    {
        // Also exercises ContextAction.ImportanceRemoval factory method
        var veryHigh = Msg(Importance.VeryHigh);
        var high     = Msg(Importance.High);
        var normal   = Msg(Importance.Normal);
        var low      = Msg(Importance.Low);
        var veryLow  = Msg(Importance.VeryLow);

        var action = ContextAction.ImportanceRemoval(Importance.Normal);
        var ctx = Context(veryHigh, high, normal, low, veryLow);
        action.Execute(ctx);

        // Normal, Low, VeryLow are at or below the threshold; VeryHigh and High remain
        Assert.HasCount(2, ctx.Messages);
        Assert.IsTrue(ctx.Messages.Contains(veryHigh));
        Assert.IsTrue(ctx.Messages.Contains(high));
    }

    // -------------------------------------------------------------------------
    // Depth
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Execute_Depth_ProtectsRecentMessages()
    {
        var old     = Msg(Importance.VeryLow); // index 0 — buried, should be removed
        var recent1 = Msg(Importance.VeryLow); // index 1 — within depth, protected
        var recent2 = Msg(Importance.VeryLow); // index 2 — within depth, protected

        var action = ContextAction.ImportanceRemoval(Importance.Normal, depth: 2);
        var ctx = Context(old, recent1, recent2);
        action.Execute(ctx);

        Assert.HasCount(2, ctx.Messages);
        Assert.IsTrue(ctx.Messages.Contains(recent1));
        Assert.IsTrue(ctx.Messages.Contains(recent2));
    }

    // -------------------------------------------------------------------------
    // Edge cases
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Execute_EmptyMessages_DoesNotThrow()
    {
        var action = ContextAction.ImportanceRemoval(Importance.Normal);
        var ctx = Context();

        action.Execute(ctx);

        Assert.IsEmpty(ctx.Messages);
    }

    [TestMethod]
    public void Execute_DepthExceedsCount_RemovesNothing()
    {
        var msg1 = Msg(Importance.VeryLow);
        var msg2 = Msg(Importance.VeryLow);

        var action = ContextAction.ImportanceRemoval(Importance.Normal, depth: 5);
        var ctx = Context(msg1, msg2);
        action.Execute(ctx);

        Assert.HasCount(2, ctx.Messages);
    }

    // -------------------------------------------------------------------------
    // JSON serialization
    // -------------------------------------------------------------------------

    [TestMethod]
    public void ImportanceRemoval_RoundTrips_ThroughJson()
    {
        var action = ContextAction.ImportanceRemoval(Importance.Low, depth: 3);

        var json = JsonSerializer.Serialize(action);
        var deserialized = JsonSerializer.Deserialize<ContextAction>(json);

        Assert.AreEqual(action, deserialized);
    }
}
