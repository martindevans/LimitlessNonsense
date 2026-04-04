using LimitlessNonsense.Cleanup;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class CleanupContextTests
{
    private static readonly Guid GuidA = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid GuidB = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid GuidC = new("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static ContextState DefaultState()
        => new(GuidA, TokenCount: 50, ContextSize: 100);

    

    private static ContextMessage Msg(Guid id, MessageRole role = MessageRole.User, Importance importance = Importance.Normal)
        => new(id, role, importance);

    // -------------------------------------------------------------------------
    // Construction
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Constructor_SetsAllProperties()
    {
        var condition = Condition.True();
        var state = DefaultState();
        IReadOnlyList<ContextMessage> messages = [Msg(GuidA), Msg(GuidB)];

        var ctx = new CleanupContext(condition, state, messages);

        Assert.AreEqual(condition, ctx.Condition);
        Assert.AreEqual(state, ctx.State);
        CollectionAssert.AreEqual(messages.ToList(), ctx.Messages.ToList());
    }

    [TestMethod]
    public void Constructor_CopiesMessages_ChangingOriginalHasNoEffect()
    {
        var list = new List<ContextMessage> { Msg(GuidA) };
        var ctx = new CleanupContext(Condition.True(), DefaultState(), list);

        list.Add(Msg(GuidB));

        Assert.HasCount(1, ctx.Messages);
    }

    [TestMethod]
    public void Constructor_EmptyMessages_IsAllowed()
    {
        var ctx = new CleanupContext(Condition.True(), DefaultState(), []);

        Assert.IsEmpty(ctx.Messages);
        Assert.IsFalse(ctx.Removals.Any());
    }

    // -------------------------------------------------------------------------
    // Remove
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Remove_RemovesFromMessages_AndTracksIdInRemovals()
    {
        var msg = Msg(GuidA);
        var ctx = new CleanupContext(Condition.True(), DefaultState(), [msg]);

        ctx.Remove(msg);

        Assert.IsEmpty(ctx.Messages);
        CollectionAssert.AreEquivalent(new[] { GuidA }, ctx.Removals.ToList());
    }

    [TestMethod]
    public void Remove_MultipleMessages_TracksAll()
    {
        var msgA = Msg(GuidA);
        var msgB = Msg(GuidB);
        var msgC = Msg(GuidC);
        var ctx = new CleanupContext(Condition.True(), DefaultState(), [msgA, msgB, msgC]);

        ctx.Remove(msgA);
        ctx.Remove(msgC);

        CollectionAssert.AreEquivalent(new[] { msgB }, ctx.Messages.ToList());
        CollectionAssert.AreEquivalent(new[] { GuidA, GuidC }, ctx.Removals.ToList());
    }

    // -------------------------------------------------------------------------
    // Edge cases
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Remove_SameMessageTwice_IdTrackedOnce_MessagesUnchangedOnSecondCall()
    {
        var msg = Msg(GuidA);
        var ctx = new CleanupContext(Condition.True(), DefaultState(), [msg]);

        ctx.Remove(msg);
        ctx.Remove(msg); // second call — message is already gone

        Assert.IsEmpty(ctx.Messages);
        Assert.AreEqual(1, ctx.Removals.Count());
        Assert.AreEqual(GuidA, ctx.Removals.Single());
    }

    [TestMethod]
    public void Remove_MessageNotInList_IdStillTracked()
    {
        var inList = Msg(GuidA);
        var notInList = Msg(GuidB);
        var ctx = new CleanupContext(Condition.True(), DefaultState(), [inList]);

        ctx.Remove(notInList);

        // Messages is unchanged because the message wasn't in the list
        CollectionAssert.AreEquivalent(new[] { inList }, ctx.Messages.ToList());
        // But the ID is still tracked in Removals
        CollectionAssert.AreEquivalent(new[] { GuidB }, ctx.Removals.ToList());
    }
}
