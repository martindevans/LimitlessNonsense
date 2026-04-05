using LimitlessNonsense.Metadata;
using LimitlessNonsense.Middleware;
using LimitlessNonsense.Middleware.Time;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class DateChangedMessageTests
{
    private static readonly DateTime Day1 = new(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Day2 = new(2024, 6, 2, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Day3 = new(2024, 6, 3, 12, 0, 0, DateTimeKind.Utc);

    private static ContextMessage DatedMsg(DateTime time)
    {
        var msg = new ContextMessage(MessageRole.User);
        msg.SetMetadata(new MessageCreationTime(time));
        return msg;
    }

    private static MiddlewareContext Context(DateTime now, params ContextMessage[] history)
    {
        var newMsg = new ContextMessage(MessageRole.User);
        return new MiddlewareContext([.. history], now, newMsg);
    }

    private static Task NoOp(MiddlewareContext _) => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // No message added
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_EmptyHistory_DoesNotAddMessage()
    {
        var middleware = new DateChangedMessage();
        var context = Context(Day2);

        await middleware.Process(context, NoOp);

        Assert.IsEmpty(context.History);
    }

    [TestMethod]
    public async Task Process_HistoryWithNoDatedMessages_DoesNotAddMessage()
    {
        var undated = new ContextMessage(MessageRole.User, content: "hello");
        var middleware = new DateChangedMessage();
        var context = Context(Day2, undated);

        await middleware.Process(context, NoOp);

        Assert.HasCount(1, context.History);
        CollectionAssert.Contains(context.History, undated);
    }

    [TestMethod]
    public async Task Process_DateUnchanged_DoesNotAddMessage()
    {
        var existing = DatedMsg(Day1);
        var middleware = new DateChangedMessage();
        var context = Context(Day1.AddHours(1), existing);

        await middleware.Process(context, NoOp);

        Assert.HasCount(1, context.History);
    }

    // -------------------------------------------------------------------------
    // Message added
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_DateChanged_AddsOneMessage()
    {
        var existing = DatedMsg(Day1);
        var middleware = new DateChangedMessage();
        var context = Context(Day2, existing);

        await middleware.Process(context, NoOp);

        Assert.HasCount(2, context.History);
    }

    [TestMethod]
    public async Task Process_DateChanged_AddedMessageHasCorrectRoleAndImportance()
    {
        var existing = DatedMsg(Day1);
        var middleware = new DateChangedMessage();
        var context = Context(Day2, existing);

        await middleware.Process(context, NoOp);

        var added = context.History.First(m => m != existing);
        Assert.AreEqual(MessageRole.Tool, added.Role);
        Assert.AreEqual(Importance.Low, added.Importance);
    }

    [TestMethod]
    public async Task Process_DateChanged_AddedMessageHasCorrectDefaultContent()
    {
        var existing = DatedMsg(Day1);
        var middleware = new DateChangedMessage();
        var context = Context(Day2, existing);

        await middleware.Process(context, NoOp);

        var added = context.History.First(m => m != existing);
        Assert.AreEqual("The date is now Sun 02-Jun-2024", added.Content);
    }

    [TestMethod]
    public async Task Process_DateChanged_AddedMessageHasCustomPrefixFormatSuffix()
    {
        var existing = DatedMsg(Day1);
        var middleware = new DateChangedMessage(prefix: "Date: ", format: "yyyy-MM-dd", suffix: ".");
        var context = Context(Day2, existing);

        await middleware.Process(context, NoOp);

        var added = context.History.First(m => m != existing);
        Assert.AreEqual("Date: 2024-06-02.", added.Content);
    }

    [TestMethod]
    public async Task Process_DateChanged_AddedMessageIsTaggedWithDate()
    {
        var existing = DatedMsg(Day1);
        var middleware = new DateChangedMessage();
        var context = Context(Day2, existing);

        await middleware.Process(context, NoOp);

        var added = context.History.First(m => m != existing);
        Assert.IsTrue(added.HasMetadata<MessageCreationTime>());
        Assert.AreEqual(DateOnly.FromDateTime(Day2), DateOnly.FromDateTime(added.TryGetMetadata<MessageCreationTime>()!.Time));
    }

    // -------------------------------------------------------------------------
    // No duplicate message once added
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_DateChanged_SecondCallSameDay_DoesNotAddAnotherMessage()
    {
        var existing = DatedMsg(Day1);
        var middleware = new DateChangedMessage();
        var context = Context(Day2, existing);

        // First call adds the date-changed message
        await middleware.Process(context, NoOp);
        Assert.HasCount(2, context.History);

        // Second call at the same timestamp should not add again
        var context2 = Context(Day2, [.. context.History]);
        await middleware.Process(context2, NoOp);

        Assert.HasCount(2, context2.History);
    }

    [TestMethod]
    public async Task Process_DateChanged_SubsequentDateChange_AddsAnotherMessage()
    {
        var existing = DatedMsg(Day1);
        var middleware = new DateChangedMessage();
        var context = Context(Day2, existing);

        await middleware.Process(context, NoOp);
        Assert.HasCount(2, context.History);

        // Now the date changes again to Day3
        var context2 = Context(Day3, [.. context.History]);
        await middleware.Process(context2, NoOp);

        Assert.HasCount(3, context2.History);
    }

    // -------------------------------------------------------------------------
    // Simultaneous messages
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_SimultaneousMessages_OnlyOneDateChangedMessageAdded()
    {
        // Two messages processed at the same timestamp, both seeing the date change from Day1 to Day2.
        // The first Process call adds a date-changed message; the second sees it and should not add another.
        var existing = DatedMsg(Day1);
        var middleware = new DateChangedMessage();
        var context = Context(Day2, existing);

        // Simulate both messages being processed against the same evolving history
        await middleware.Process(context, NoOp);
        await middleware.Process(context, NoOp);

        var dateChangedMessages = context.History.Where(m => m != existing).ToList();
        Assert.HasCount(1, dateChangedMessages);
    }

    // -------------------------------------------------------------------------
    // Uses last dated message (not first)
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_MultipleOldDatedMessages_UsesLastOne_NoMessageAdded()
    {
        // Oldest message is Day1, most recent is Day2. Current time is also Day2.
        // Should look at the last dated message (Day2), see no change, and not add.
        var old = DatedMsg(Day1);
        var recent = DatedMsg(Day2);
        var middleware = new DateChangedMessage();
        var context = Context(Day2.AddHours(1), old, recent);

        await middleware.Process(context, NoOp);

        Assert.HasCount(2, context.History);
    }

    [TestMethod]
    public async Task Process_MultipleOldDatedMessages_UsesLastOne_MessageAdded()
    {
        // Oldest message is Day1, most recent is Day2. Current time is Day3.
        // Should look at the last dated message (Day2) and add a date-change message for Day3.
        var old = DatedMsg(Day1);
        var recent = DatedMsg(Day2);
        var middleware = new DateChangedMessage();
        var context = Context(Day3, old, recent);

        await middleware.Process(context, NoOp);

        Assert.HasCount(3, context.History);
    }

    // -------------------------------------------------------------------------
    // Next is always called
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_DateUnchanged_StillCallsNext()
    {
        var existing = DatedMsg(Day1);
        var middleware = new DateChangedMessage();
        var context = Context(Day1, existing);
        var called = false;

        await middleware.Process(context, _ => { called = true; return Task.CompletedTask; });

        Assert.IsTrue(called);
    }

    [TestMethod]
    public async Task Process_DateChanged_StillCallsNext()
    {
        var existing = DatedMsg(Day1);
        var middleware = new DateChangedMessage();
        var context = Context(Day2, existing);
        var called = false;

        await middleware.Process(context, _ => { called = true; return Task.CompletedTask; });

        Assert.IsTrue(called);
    }

    [TestMethod]
    public async Task Process_EmptyHistory_StillCallsNext()
    {
        var middleware = new DateChangedMessage();
        var context = Context(Day1);
        var called = false;

        await middleware.Process(context, _ => { called = true; return Task.CompletedTask; });

        Assert.IsTrue(called);
    }
}
