using LimitlessNonsense.Middleware;
using LimitlessNonsense.Middleware.Metadata;
using LimitlessNonsense.Middleware.Time;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class ElapsedTimeMessageTests
{
    private static readonly TimeSpan Threshold = TimeSpan.FromHours(1);
    private static readonly DateTime BaseTime = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static MiddlewareContext Context(DateTime now, params ContextMessage[] history)
        => new(new List<ContextMessage>(history), now, new ContextMessage(MessageRole.User));

    private static ContextMessage MessageWithTime(DateTime time, MessageRole role = MessageRole.User)
    {
        var msg = new ContextMessage(role);
        msg.SetMetadata(new MessageCreationTime(time));
        return msg;
    }

    private static Task NoOp(MiddlewareContext _) => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Below Threshold
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_ElapsedBelowThreshold_DoesNotAddMessage()
    {
        var last = MessageWithTime(BaseTime);
        var ctx = Context(BaseTime + TimeSpan.FromMinutes(30), last);

        await new ElapsedTimeMessage(Threshold).Process(ctx, NoOp);

        Assert.HasCount(1, ctx.History);
    }

    [TestMethod]
    public async Task Process_ElapsedExactlyAtThreshold_DoesNotAddMessage()
    {
        var last = MessageWithTime(BaseTime);
        var ctx = Context(BaseTime + Threshold, last);

        await new ElapsedTimeMessage(Threshold).Process(ctx, NoOp);

        Assert.HasCount(1, ctx.History);
    }

    // -------------------------------------------------------------------------
    // Above Threshold
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_ElapsedAboveThreshold_AddsMessage()
    {
        var last = MessageWithTime(BaseTime);
        var ctx = Context(BaseTime + TimeSpan.FromHours(2), last);

        await new ElapsedTimeMessage(Threshold).Process(ctx, NoOp);

        Assert.HasCount(2, ctx.History);
    }

    [TestMethod]
    public async Task Process_ElapsedAboveThreshold_AddedMessageIsEphemeral()
    {
        var last = MessageWithTime(BaseTime);
        var ctx = Context(BaseTime + TimeSpan.FromHours(2), last);

        await new ElapsedTimeMessage(Threshold).Process(ctx, NoOp);

        Assert.AreEqual(Importance.Ephemeral, ctx.History[^1].Importance);
    }

    [TestMethod]
    public async Task Process_ElapsedAboveThreshold_AddedMessageIsToolRole()
    {
        var last = MessageWithTime(BaseTime);
        var ctx = Context(BaseTime + TimeSpan.FromHours(2), last);

        await new ElapsedTimeMessage(Threshold).Process(ctx, NoOp);

        Assert.AreEqual(MessageRole.Tool, ctx.History[^1].Role);
    }

    [TestMethod]
    public async Task Process_ElapsedAboveThreshold_MessageContentMentionsSinceLastMessage()
    {
        var last = MessageWithTime(BaseTime);
        var ctx = Context(BaseTime + TimeSpan.FromHours(2), last);

        await new ElapsedTimeMessage(Threshold).Process(ctx, NoOp);

        Assert.IsTrue(ctx.History[^1].Content.Contains("since last message", StringComparison.Ordinal));
    }

    // -------------------------------------------------------------------------
    // Not Added Twice
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_CalledTwice_HistoryHasOnlyOneElapsedMessage()
    {
        var last = MessageWithTime(BaseTime);
        var ctx = Context(BaseTime + TimeSpan.FromHours(2), last);
        var middleware = new ElapsedTimeMessage(Threshold);

        await middleware.Process(ctx, NoOp);
        await middleware.Process(ctx, NoOp);

        // Original message plus exactly one elapsed message
        Assert.HasCount(2, ctx.History);
    }

    [TestMethod]
    public async Task Process_CalledTwice_ReplacesExistingElapsedMessage()
    {
        var last = MessageWithTime(BaseTime);
        var ctx = Context(BaseTime + TimeSpan.FromHours(2), last);
        var middleware = new ElapsedTimeMessage(Threshold);

        await middleware.Process(ctx, NoOp);
        var firstElapsedMsg = ctx.History[^1];

        await middleware.Process(ctx, NoOp);

        Assert.DoesNotContain(firstElapsedMsg, ctx.History);
    }

    // -------------------------------------------------------------------------
    // Edge Cases
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_EmptyHistory_DoesNotAddMessage()
    {
        var ctx = Context(BaseTime);

        await new ElapsedTimeMessage(Threshold).Process(ctx, NoOp);

        Assert.IsEmpty(ctx.History);
    }

    [TestMethod]
    public async Task Process_LastMessageHasNoCreationTimeMetadata_DoesNotAddMessage()
    {
        var msgWithoutTime = new ContextMessage(MessageRole.User);
        var ctx = Context(BaseTime + TimeSpan.FromHours(2), msgWithoutTime);

        await new ElapsedTimeMessage(Threshold).Process(ctx, NoOp);

        Assert.HasCount(1, ctx.History);
    }

    [TestMethod]
    public async Task Process_SimultaneousMessages_ZeroElapsed_DoesNotAddMessage()
    {
        // Two messages at the same instant: elapsed == 0, which is below any positive threshold
        var last = MessageWithTime(BaseTime);
        var ctx = Context(BaseTime, last);

        await new ElapsedTimeMessage(Threshold).Process(ctx, NoOp);

        Assert.HasCount(1, ctx.History);
    }

    [TestMethod]
    public async Task Process_MultipleMessagesInHistory_UsesLastMessageTime()
    {
        // Older message is far in the past, but the last message is recent
        var oldMsg = MessageWithTime(BaseTime);
        var recentMsg = MessageWithTime(BaseTime + TimeSpan.FromHours(3));
        // Only 10 minutes have passed since the most recent message
        var ctx = Context(BaseTime + TimeSpan.FromHours(3) + TimeSpan.FromMinutes(10), oldMsg, recentMsg);

        await new ElapsedTimeMessage(Threshold).Process(ctx, NoOp);

        // 10 minutes is below the 1-hour threshold — no elapsed message added
        Assert.HasCount(2, ctx.History);
    }
}
