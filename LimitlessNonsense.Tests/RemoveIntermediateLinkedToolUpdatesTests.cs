using System.Text.Json;
using LimitlessNonsense.Cleanup;
using LimitlessNonsense.Cleanup.Actions;
using LimitlessNonsense.Metadata;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class RemoveIntermediateLinkedToolUpdatesTests
{
    private static CleanupContext Context(params Message[] messages)
        => new(Condition.True(), new ContextState(Guid.NewGuid(), 100, 200), messages.ToList());

    private static Message Linked(Guid id, LinkedToolCallType type)
    {
        var msg = new Message(MessageRole.Tool);
        msg.SetMetadata(new LinkedToolCall(id, type));
        return msg;
    }

    private static Message Unlinked()
        => new Message(MessageRole.User);

    // -------------------------------------------------------------------------
    // No-op cases
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Execute_EmptyMessages_DoesNothing()
    {
        var ctx = Context();

        var changed = await ContextAction.RemoveIntermediateLinkedToolUpdates().Execute(ctx);

        Assert.IsFalse(changed);
        Assert.IsEmpty(ctx.Messages);
    }

    [TestMethod]
    public async Task Execute_NoLinkedMetadata_DoesNothing()
    {
        var a = Unlinked();
        var b = Unlinked();
        var ctx = Context(a, b);

        var changed = await ContextAction.RemoveIntermediateLinkedToolUpdates().Execute(ctx);

        Assert.IsFalse(changed);
        Assert.HasCount(2, ctx.Messages);
    }

    [TestMethod]
    public async Task Execute_SingleUpdate_NoOtherMessages_DoesNothing()
    {
        var id = Guid.NewGuid();
        var update = Linked(id, LinkedToolCallType.Update);
        var ctx = Context(update);

        var changed = await ContextAction.RemoveIntermediateLinkedToolUpdates().Execute(ctx);

        Assert.IsFalse(changed);
        Assert.HasCount(1, ctx.Messages);
    }

    [TestMethod]
    public async Task Execute_SingleCallOnly_DoesNothing()
    {
        var id = Guid.NewGuid();
        var call = Linked(id, LinkedToolCallType.Call);
        var ctx = Context(call);

        var changed = await ContextAction.RemoveIntermediateLinkedToolUpdates().Execute(ctx);

        Assert.IsFalse(changed);
        Assert.HasCount(1, ctx.Messages);
    }

    // -------------------------------------------------------------------------
    // Multiple updates collapse — no final result
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Execute_MultipleUpdates_NoResult_KeepsMostRecentUpdate()
    {
        var id = Guid.NewGuid();
        var call    = Linked(id, LinkedToolCallType.Call);
        var update1 = Linked(id, LinkedToolCallType.Update);
        var update2 = Linked(id, LinkedToolCallType.Update);
        var update3 = Linked(id, LinkedToolCallType.Update);
        var ctx = Context(call, update1, update2, update3);

        var changed = await ContextAction.RemoveIntermediateLinkedToolUpdates().Execute(ctx);

        Assert.IsTrue(changed);
        // Only call and the most-recent update (update3) should survive
        Assert.HasCount(2, ctx.Messages);
        Assert.Contains(call, ctx.Messages);
        Assert.Contains(update3, ctx.Messages);
        Assert.DoesNotContain(update1, ctx.Messages);
        Assert.DoesNotContain(update2, ctx.Messages);
    }

    // -------------------------------------------------------------------------
    // Updates with a final result — all updates removed
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Execute_UpdatesWithResult_RemovesAllUpdates()
    {
        var id = Guid.NewGuid();
        var call    = Linked(id, LinkedToolCallType.Call);
        var update1 = Linked(id, LinkedToolCallType.Update);
        var update2 = Linked(id, LinkedToolCallType.Update);
        var result  = Linked(id, LinkedToolCallType.Result);
        var ctx = Context(call, update1, update2, result);

        var changed = await ContextAction.RemoveIntermediateLinkedToolUpdates().Execute(ctx);

        Assert.IsTrue(changed);
        Assert.HasCount(2, ctx.Messages);
        Assert.Contains(call, ctx.Messages);
        Assert.Contains(result, ctx.Messages);
        Assert.DoesNotContain(update1, ctx.Messages);
        Assert.DoesNotContain(update2, ctx.Messages);
    }

    [TestMethod]
    public async Task Execute_ResultOnly_NoUpdates_DoesNothing()
    {
        var id = Guid.NewGuid();
        var call   = Linked(id, LinkedToolCallType.Call);
        var result = Linked(id, LinkedToolCallType.Result);
        var ctx = Context(call, result);

        var changed = await ContextAction.RemoveIntermediateLinkedToolUpdates().Execute(ctx);

        Assert.IsFalse(changed);
        Assert.HasCount(2, ctx.Messages);
    }

    // -------------------------------------------------------------------------
    // Multiple independent tool-call ids don't interfere
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Execute_TwoIndependentIds_EachCollapsesIndependently()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var call1    = Linked(id1, LinkedToolCallType.Call);
        var update1a = Linked(id1, LinkedToolCallType.Update);
        var update1b = Linked(id1, LinkedToolCallType.Update);

        var call2    = Linked(id2, LinkedToolCallType.Call);
        var update2a = Linked(id2, LinkedToolCallType.Update);
        var result2  = Linked(id2, LinkedToolCallType.Result);

        var ctx = Context(call1, update1a, update1b, call2, update2a, result2);

        var changed = await ContextAction.RemoveIntermediateLinkedToolUpdates().Execute(ctx);

        Assert.IsTrue(changed);

        // id1: no result, keep call1 + most-recent update (update1b)
        Assert.Contains(call1, ctx.Messages);
        Assert.Contains(update1b, ctx.Messages);
        Assert.DoesNotContain(update1a, ctx.Messages);

        // id2: has result, remove update2a
        Assert.Contains(call2, ctx.Messages);
        Assert.Contains(result2, ctx.Messages);
        Assert.DoesNotContain(update2a, ctx.Messages);

        Assert.HasCount(4, ctx.Messages);
    }

    [TestMethod]
    public async Task Execute_UnlinkedMessagesUnaffected()
    {
        var id = Guid.NewGuid();
        var before  = Unlinked();
        var call    = Linked(id, LinkedToolCallType.Call);
        var update1 = Linked(id, LinkedToolCallType.Update);
        var update2 = Linked(id, LinkedToolCallType.Update);
        var after   = Unlinked();
        var ctx = Context(before, call, update1, update2, after);

        var changed = await ContextAction.RemoveIntermediateLinkedToolUpdates().Execute(ctx);

        Assert.IsTrue(changed);
        Assert.Contains(before, ctx.Messages);
        Assert.Contains(call, ctx.Messages);
        Assert.Contains(update2, ctx.Messages);
        Assert.Contains(after, ctx.Messages);
        Assert.DoesNotContain(update1, ctx.Messages);
        Assert.HasCount(4, ctx.Messages);
    }

    // -------------------------------------------------------------------------
    // JSON serialization
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RemoveIntermediateLinkedToolUpdates_SerializesWithCorrectDiscriminator_AndRoundTrips()
    {
        var action = ContextAction.RemoveIntermediateLinkedToolUpdates();

        var json = JsonSerializer.Serialize(action);
        var deserialized = JsonSerializer.Deserialize<ContextAction>(json);

        StringAssert.Contains(json, "\"$type\":\"RemoveIntermediateLinkedToolUpdates\"");
        Assert.AreEqual(action, deserialized);
    }
}
