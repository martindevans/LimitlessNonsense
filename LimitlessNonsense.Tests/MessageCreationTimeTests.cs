using LimitlessNonsense.Middleware;
using LimitlessNonsense.Middleware.Metadata;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class AddMessageCreationTimeMetadataTests
{
    private static readonly DateTime Now = new(2024, 6, 1, 13, 46, 0, DateTimeKind.Utc);

    private static MiddlewareContext Context(DateTime now, ContextMessage? message = null)
    {
        message ??= new ContextMessage(MessageRole.User);
        return new MiddlewareContext([], now, message);
    }

    private static Task NoOp(MiddlewareContext _) => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Metadata added
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_NoExistingMetadata_AddsCreationTimeMetadata()
    {
        var message = new ContextMessage(MessageRole.User);
        var context = Context(Now, message);

        await new AddMessageCreationTimeMetadata().Process(context, NoOp);

        Assert.IsTrue(message.HasMetadata<MessageCreationTime>());
    }

    [TestMethod]
    public async Task Process_NoExistingMetadata_MetadataTimeMatchesContextNow()
    {
        var message = new ContextMessage(MessageRole.User);
        var context = Context(Now, message);

        await new AddMessageCreationTimeMetadata().Process(context, NoOp);

        Assert.AreEqual(Now, message.TryGetMetadata<MessageCreationTime>()!.Time);
    }

    // -------------------------------------------------------------------------
    // Overwrite behaviour
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_ExistingMetadata_OverwriteFalse_DoesNotOverwrite()
    {
        var earlier = Now.AddHours(-1);
        var message = new ContextMessage(MessageRole.User);
        message.SetMetadata(new MessageCreationTime(earlier));
        var context = Context(Now, message);

        await new AddMessageCreationTimeMetadata(overwrite: false).Process(context, NoOp);

        Assert.AreEqual(earlier, message.TryGetMetadata<MessageCreationTime>()!.Time);
    }

    [TestMethod]
    public async Task Process_ExistingMetadata_OverwriteTrue_OverwritesMetadata()
    {
        var earlier = Now.AddHours(-1);
        var message = new ContextMessage(MessageRole.User);
        message.SetMetadata(new MessageCreationTime(earlier));
        var context = Context(Now, message);

        await new AddMessageCreationTimeMetadata(overwrite: true).Process(context, NoOp);

        Assert.AreEqual(Now, message.TryGetMetadata<MessageCreationTime>()!.Time);
    }

    // -------------------------------------------------------------------------
    // Next is always called
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_NoExistingMetadata_CallsNext()
    {
        var context = Context(Now);
        var called = false;

        await new AddMessageCreationTimeMetadata().Process(context, ctx => { called = true; return Task.CompletedTask; });

        Assert.IsTrue(called);
    }

    [TestMethod]
    public async Task Process_ExistingMetadata_OverwriteFalse_CallsNext()
    {
        var message = new ContextMessage(MessageRole.User);
        message.SetMetadata(new MessageCreationTime(Now));
        var context = Context(Now, message);
        var called = false;

        await new AddMessageCreationTimeMetadata(overwrite: false).Process(context, ctx => { called = true; return Task.CompletedTask; });

        Assert.IsTrue(called);
    }
}

[TestClass]
public sealed class AddMessageTimePrefixTests
{
    private static readonly DateTime Now = new(2024, 6, 1, 13, 46, 0, DateTimeKind.Utc);

    private static MiddlewareContext ContextWithMessage(ContextMessage message)
        => new([], Now, message);

    private static ContextMessage MessageWithCreationTime(DateTime time, string prefix = "")
    {
        var msg = new ContextMessage(MessageRole.User);
        msg.SetMetadata(new MessageCreationTime(time));
        msg.Prefix = prefix;
        return msg;
    }

    private static Task NoOp(MiddlewareContext _) => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // No metadata — prefix unchanged
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_NoCreationTimeMetadata_DoesNotModifyPrefix()
    {
        var message = new ContextMessage(MessageRole.User);
        var context = ContextWithMessage(message);

        await new AddMessageTimePrefix().Process(context, NoOp);

        Assert.AreEqual("", message.Prefix);
    }

    [TestMethod]
    public async Task Process_NoCreationTimeMetadata_CallsNext()
    {
        var message = new ContextMessage(MessageRole.User);
        var context = ContextWithMessage(message);
        var called = false;

        await new AddMessageTimePrefix().Process(context, ctx => { called = true; return Task.CompletedTask; });

        Assert.IsTrue(called);
    }

    // -------------------------------------------------------------------------
    // Prefix formatting
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_WithCreationTimeMetadata_AddsTimePrefixToMessage()
    {
        var message = MessageWithCreationTime(Now);
        var context = ContextWithMessage(message);

        await new AddMessageTimePrefix().Process(context, NoOp);

        Assert.IsGreaterThan(0, message.Prefix.Length);
    }

    [TestMethod]
    public async Task Process_DefaultFormat_ProducesExpectedPrefix()
    {
        var message = MessageWithCreationTime(Now);
        var context = ContextWithMessage(message);

        await new AddMessageTimePrefix().Process(context, NoOp);

        // Default: format="t", pre="[", post="]" — "t" with InvariantCulture gives HH:mm
        Assert.AreEqual("[13:46]", message.Prefix);
    }

    [TestMethod]
    public async Task Process_CustomFormat_UsesCustomFormat()
    {
        var message = MessageWithCreationTime(Now);
        var context = ContextWithMessage(message);

        await new AddMessageTimePrefix(format: "HH:mm:ss").Process(context, NoOp);

        Assert.AreEqual("[13:46:00]", message.Prefix);
    }

    [TestMethod]
    public async Task Process_CustomPrePost_UsesCustomBrackets()
    {
        var message = MessageWithCreationTime(Now);
        var context = ContextWithMessage(message);

        await new AddMessageTimePrefix(pre: "(", post: ")").Process(context, NoOp);

        Assert.AreEqual("(13:46)", message.Prefix);
    }

    [TestMethod]
    public async Task Process_EmptyPrePost_ProducesJustFormattedTime()
    {
        var message = MessageWithCreationTime(Now);
        var context = ContextWithMessage(message);

        await new AddMessageTimePrefix(pre: "", post: "").Process(context, NoOp);

        Assert.AreEqual("13:46", message.Prefix);
    }

    // -------------------------------------------------------------------------
    // Existing prefix is preserved
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_ExistingPrefix_TimePrependedBeforeExistingPrefix()
    {
        var message = MessageWithCreationTime(Now, prefix: "existing");
        var context = ContextWithMessage(message);

        await new AddMessageTimePrefix().Process(context, NoOp);

        Assert.AreEqual("[13:46]existing", message.Prefix);
    }

    // -------------------------------------------------------------------------
    // Next is always called
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_WithCreationTimeMetadata_CallsNext()
    {
        var message = MessageWithCreationTime(Now);
        var context = ContextWithMessage(message);
        var called = false;

        await new AddMessageTimePrefix().Process(context, ctx => { called = true; return Task.CompletedTask; });

        Assert.IsTrue(called);
    }
}
