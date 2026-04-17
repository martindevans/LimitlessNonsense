using System.Text.Json;
using LimitlessNonsense.Cleanup;
using LimitlessNonsense.Cleanup.Actions;
using LimitlessNonsense.Services;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class AutoSummaryTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static CleanupContext Context(
        IServiceProvider? services,
        params Message[] messages)
        => new(Condition.True(), new ContextState(Guid.NewGuid(), 50, 100), messages.ToList(), services);

    private static Message Msg(MessageRole role, string content = "")
        => new Message(role, content: content);

    /// <summary>Minimal IServiceProvider that exposes a single ISummarisationProvider.</summary>
    private sealed class FakeServices(ISummarisationProvider provider) : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => serviceType == typeof(ISummarisationProvider) ? provider : null;
    }

    /// <summary>Controllable ISummarisationProvider backed by a TaskCompletionSource.</summary>
    private sealed class FakeProvider : ISummarisationProvider
    {
        private readonly TaskCompletionSource<string> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string? ReceivedTranscript { get; private set; }

        public Task<string> Summarise(string input, CancellationToken cancellation = default)
        {
            ReceivedTranscript = input;
            cancellation.Register(() => _tcs.TrySetCanceled());
            return _tcs.Task;
        }

        public void Complete(string result) => _tcs.TrySetResult(result);
    }

    // -------------------------------------------------------------------------
    // BeginSummarise – no-op cases
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task BeginSummarise_AlreadyHasActiveTask_DoesNotStartNewTask()
    {
        var provider = new FakeProvider();
        var ctx = Context(new FakeServices(provider), Msg(MessageRole.User, "a"), Msg(MessageRole.User, "b"));

        // Seed an existing task
        var existingTask = new SummarisationTask([], Task.FromResult("existing"), new CancellationTokenSource());
        ctx.ActiveSummarisationTask = existingTask;

        var changed = await ContextAction.BeginSummarise(keepEnd: 0).Execute(ctx);

        Assert.IsFalse(changed);
        Assert.AreSame(existingTask, ctx.ActiveSummarisationTask);
    }

    [TestMethod]
    public async Task BeginSummarise_NoProvider_DoesNothing()
    {
        var ctx = Context(services: null, Msg(MessageRole.User, "hello"));

        var changed = await ContextAction.BeginSummarise(keepEnd: 0).Execute(ctx);

        Assert.IsFalse(changed);
        Assert.IsNull(ctx.ActiveSummarisationTask);
    }

    [TestMethod]
    public async Task BeginSummarise_EmptyRange_DoesNothing()
    {
        var provider = new FakeProvider();
        // keepEnd covers all messages so the range is empty
        var ctx = Context(new FakeServices(provider), Msg(MessageRole.User), Msg(MessageRole.User));

        var changed = await ContextAction.BeginSummarise(keepStart: 0, keepEnd: 2).Execute(ctx);

        Assert.IsFalse(changed);
        Assert.IsNull(ctx.ActiveSummarisationTask);
    }

    // -------------------------------------------------------------------------
    // BeginSummarise – task is started
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task BeginSummarise_ValidContext_StartsTaskAndReturnsFalse()
    {
        var provider = new FakeProvider();
        var ctx = Context(new FakeServices(provider), Msg(MessageRole.User, "msg1"), Msg(MessageRole.User, "msg2"));

        var changed = await ContextAction.BeginSummarise(keepEnd: 0).Execute(ctx);

        Assert.IsFalse(changed);
        Assert.IsNotNull(ctx.ActiveSummarisationTask);
    }

    [TestMethod]
    public async Task BeginSummarise_KeepStartAndEnd_SlicesRangeCorrectly()
    {
        var provider = new FakeProvider();
        var m0 = Msg(MessageRole.User, "keep-start");
        var m1 = Msg(MessageRole.User, "in-range-1");
        var m2 = Msg(MessageRole.User, "in-range-2");
        var m3 = Msg(MessageRole.User, "keep-end");
        var ctx = Context(new FakeServices(provider), m0, m1, m2, m3);

        await ContextAction.BeginSummarise(keepStart: 1, keepEnd: 1).Execute(ctx);

        Assert.IsNotNull(provider.ReceivedTranscript);

        // m1 and m2 are in the summarised range; m0 and m3 are not
        StringAssert.Contains(provider.ReceivedTranscript, "in-range-1");
        StringAssert.Contains(provider.ReceivedTranscript, "in-range-2");
        Assert.DoesNotContain("keep-start", provider.ReceivedTranscript);
        Assert.DoesNotContain("keep-end", provider.ReceivedTranscript);

        // IDs of m1 and m2 should be stored
        var task = ctx.ActiveSummarisationTask!;
        CollectionAssert.Contains(task.Messages.ToList(), m1.ID);
        CollectionAssert.Contains(task.Messages.ToList(), m2.ID);
    }

    [TestMethod]
    public async Task BeginSummarise_PreserveSystemStart_SkipsLeadingSystemMessages()
    {
        var provider = new FakeProvider();
        var sys  = Msg(MessageRole.System, "system-content");
        var user = Msg(MessageRole.User,   "user-content");
        var ctx = Context(new FakeServices(provider), sys, user);

        await ContextAction.BeginSummarise(keepEnd: 0, preserveSystemStart: true).Execute(ctx);

        // The System message at position 0 of the range should be excluded from transcript
        Assert.IsNotNull(provider.ReceivedTranscript);
        Assert.DoesNotContain("system-content", provider.ReceivedTranscript!);
        StringAssert.Contains(provider.ReceivedTranscript, "user-content");
    }

    [TestMethod]
    public async Task BeginSummarise_PreserveSystemStartFalse_IncludesSystemMessages()
    {
        var provider = new FakeProvider();
        var sys  = Msg(MessageRole.System, "system-content");
        var user = Msg(MessageRole.User,   "user-content");
        var ctx = Context(new FakeServices(provider), sys, user);

        await ContextAction.BeginSummarise(keepEnd: 0, preserveSystemStart: false).Execute(ctx);

        Assert.IsNotNull(provider.ReceivedTranscript);
        StringAssert.Contains(provider.ReceivedTranscript, "system-content");
    }

    [TestMethod]
    public async Task BeginSummarise_DeleteRoles_ExcludesMatchingMessagesFromTranscript()
    {
        var provider = new FakeProvider();
        var reasoning = Msg(MessageRole.Reasoning, "reasoning-content");
        var user      = Msg(MessageRole.User,      "user-content");
        var ctx = Context(new FakeServices(provider), reasoning, user);

        await ContextAction.BeginSummarise(keepEnd: 0, deleteRoles: MessageRole.Reasoning).Execute(ctx);

        Assert.IsNotNull(provider.ReceivedTranscript);
        Assert.DoesNotContain("reasoning-content", provider.ReceivedTranscript!);
        StringAssert.Contains(provider.ReceivedTranscript, "user-content");

        // The Reasoning message ID should still appear in the task message list
        // (it is included in range, just excluded from the text)
        var taskMessages = ctx.ActiveSummarisationTask!.Messages;
        CollectionAssert.Contains(taskMessages.ToList(), reasoning.ID);
    }

    // -------------------------------------------------------------------------
    // EndSummarise – no-op cases
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task EndSummarise_NoActiveTask_DoesNothing()
    {
        var ctx = Context(services: null, Msg(MessageRole.User));

        var changed = await ContextAction.EndSummarise(block: false).Execute(ctx);

        Assert.IsFalse(changed);
    }

    [TestMethod]
    public async Task EndSummarise_TaskInFlight_BlockFalse_DoesNotWait()
    {
        var provider = new FakeProvider();
        var ctx = Context(new FakeServices(provider), Msg(MessageRole.User, "msg"));
        await ContextAction.BeginSummarise(keepEnd: 0).Execute(ctx);

        // Task is not yet completed; Block=false should bail out immediately
        var changed = await ContextAction.EndSummarise(block: false).Execute(ctx);

        Assert.IsFalse(changed);
        Assert.IsNotNull(ctx.ActiveSummarisationTask); // task still in slot
    }

    [TestMethod]
    public async Task EndSummarise_OriginalMessagesRemoved_ReturnsFalseAndClearsSlot()
    {
        var provider = new FakeProvider();
        var msg = Msg(MessageRole.User, "original");
        var ctx = Context(new FakeServices(provider), msg);
        await ContextAction.BeginSummarise(keepEnd: 0).Execute(ctx);

        // Complete the provider task
        provider.Complete("summary text");

        // Remove the original message before EndSummarise runs
        ctx.Messages.Remove(msg);

        var changed = await ContextAction.EndSummarise(block: true).Execute(ctx);

        Assert.IsFalse(changed);
        Assert.IsNull(ctx.ActiveSummarisationTask);
    }

    // -------------------------------------------------------------------------
    // EndSummarise – successful swap
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task EndSummarise_TaskComplete_SwapsMessagesWithSummary()
    {
        var provider = new FakeProvider();
        var user1 = Msg(MessageRole.User,      "first");
        var user2 = Msg(MessageRole.User,      "second");
        var keep  = Msg(MessageRole.Assistant, "keep");
        var ctx = Context(new FakeServices(provider), user1, user2, keep);

        // Summarise the first two, keep the last one
        await ContextAction.BeginSummarise(keepEnd: 1).Execute(ctx);
        provider.Complete("summary text");

        var changed = await ContextAction.EndSummarise(block: true).Execute(ctx);

        Assert.IsTrue(changed);
        Assert.IsNull(ctx.ActiveSummarisationTask);

        // Summarised messages are gone; keep message still present
        CollectionAssert.DoesNotContain(ctx.Messages, user1);
        CollectionAssert.DoesNotContain(ctx.Messages, user2);
        CollectionAssert.Contains(ctx.Messages, keep);

        // A summary message was inserted with the correct content
        var summary = ctx.Messages.SingleOrDefault(m => m.Role == MessageRole.Summary);
        Assert.IsNotNull(summary);
        Assert.AreEqual("summary text", summary!.Content);
    }

    [TestMethod]
    public async Task EndSummarise_SummaryInsertedAtPositionOfFirstOriginalMessage()
    {
        var provider = new FakeProvider();
        var before = Msg(MessageRole.System, "system");
        var inRange = Msg(MessageRole.User,  "in-range");
        var after   = Msg(MessageRole.User,  "after");
        var ctx = Context(new FakeServices(provider), before, inRange, after);

        // keepStart=1 skips 'before'; keepEnd=1 keeps 'after'; only 'inRange' is summarised
        await ContextAction.BeginSummarise(keepStart: 1, keepEnd: 1).Execute(ctx);
        provider.Complete("summary");

        await ContextAction.EndSummarise(block: true).Execute(ctx);

        // 'before' is still at index 0; summary should be at index 1; 'after' at index 2
        Assert.HasCount(3, ctx.Messages);
        Assert.AreSame(before, ctx.Messages[0]);
        Assert.AreEqual(MessageRole.Summary, ctx.Messages[1].Role);
        Assert.AreSame(after, ctx.Messages[2]);
    }

    [TestMethod]
    public async Task EndSummarise_BlockTrue_WaitsForAsyncTask()
    {
        var provider = new FakeProvider();
        var msg = Msg(MessageRole.User, "hello");
        var ctx = Context(new FakeServices(provider), msg);
        await ContextAction.BeginSummarise(keepEnd: 0).Execute(ctx);

        // Complete the provider asynchronously
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            provider.Complete("async summary");
        });

        var changed = await ContextAction.EndSummarise(block: true).Execute(ctx);

        Assert.IsTrue(changed);
        var summary = ctx.Messages.Single(m => m.Role == MessageRole.Summary);
        Assert.AreEqual("async summary", summary.Content);
    }

    [TestMethod]
    public async Task EndSummarise_TaskCanceled_ReturnsFalseAndClearsSlot()
    {
        var provider = new FakeProvider();
        var msg = Msg(MessageRole.User, "msg");
        var ctx = Context(new FakeServices(provider), msg);
        await ContextAction.BeginSummarise(keepEnd: 0).Execute(ctx);

        // Cancel the in-flight task via the CancellationTokenSource stored in the slot
        ctx.ActiveSummarisationTask!.Cancellation.Cancel();

        var changed = await ContextAction.EndSummarise(block: true).Execute(ctx);

        Assert.IsFalse(changed);
        Assert.IsNull(ctx.ActiveSummarisationTask);
        // Original message should still be in context
        CollectionAssert.Contains(ctx.Messages, msg);
    }

    // -------------------------------------------------------------------------
    // JSON serialisation round-trips
    // -------------------------------------------------------------------------

    [TestMethod]
    public void BeginSummarise_SerializesAndDeserializesCorrectly()
    {
        var action = ContextAction.BeginSummarise(keepStart: 2, keepEnd: 4, preserveSystemStart: false, deleteRoles: MessageRole.Tool);

        var json = JsonSerializer.Serialize(action);
        var deserialized = JsonSerializer.Deserialize<ContextAction>(json);

        StringAssert.Contains(json, "\"$type\":\"BeginSummarise\"");
        Assert.AreEqual(action, deserialized);
    }

    [TestMethod]
    public void EndSummarise_SerializesAndDeserializesCorrectly()
    {
        var action = ContextAction.EndSummarise(block: true);

        var json = JsonSerializer.Serialize(action);
        var deserialized = JsonSerializer.Deserialize<ContextAction>(json);

        StringAssert.Contains(json, "\"$type\":\"EndSummarise\"");
        Assert.AreEqual(action, deserialized);
    }
}
