using LimitlessNonsense.Metadata;
using LimitlessNonsense.Middleware;
using LimitlessNonsense.Middleware.Metadata;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class AddMessageSenderPrefixTests
{
    private static Message MsgWithSender(string senderName)
    {
        var msg = new Message(MessageRole.User);
        msg.SetMetadata(new MessageSender(senderName));
        return msg;
    }

    private static MiddlewareContext<int> Context(Message message)
        => new([], DateTime.UtcNow, message, 0);

    private static Task NoOp(MiddlewareContext<int> _) => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // No sender metadata
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_NoSenderMetadata_PrefixUnchanged()
    {
        var msg = new Message(MessageRole.User);
        var middleware = new AddMessageSenderPrefix<int>();
        var context = Context(msg);

        await middleware.Process(context, NoOp);

        Assert.AreEqual("", context.Message.Prefix);
    }

    // -------------------------------------------------------------------------
    // Sender with valid name
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_SenderWithValidName_PrependsSenderPrefixWithDefaultBrackets()
    {
        var msg = MsgWithSender("Martin");
        var middleware = new AddMessageSenderPrefix<int>();
        var context = Context(msg);

        await middleware.Process(context, NoOp);

        Assert.AreEqual("[Martin]", context.Message.Prefix);
    }

    [TestMethod]
    public async Task Process_SenderWithValidName_ExistingPrefixIsPreserved()
    {
        var msg = MsgWithSender("Alice");
        msg.Prefix = "existing";
        var middleware = new AddMessageSenderPrefix<int>();
        var context = Context(msg);

        await middleware.Process(context, NoOp);

        Assert.AreEqual("[Alice]existing", context.Message.Prefix);
    }

    // -------------------------------------------------------------------------
    // Empty / whitespace sender name
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_SenderWithEmptyName_PrefixUnchanged()
    {
        var msg = MsgWithSender("");
        var middleware = new AddMessageSenderPrefix<int>();
        var context = Context(msg);

        await middleware.Process(context, NoOp);

        Assert.AreEqual("", context.Message.Prefix);
    }

    [TestMethod]
    public async Task Process_SenderWithWhitespaceName_PrefixUnchanged()
    {
        var msg = MsgWithSender("   ");
        var middleware = new AddMessageSenderPrefix<int>();
        var context = Context(msg);

        await middleware.Process(context, NoOp);

        Assert.AreEqual("", context.Message.Prefix);
    }

    // -------------------------------------------------------------------------
    // Custom delimiters
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_CustomDelimiters_UsesCustomPreAndPost()
    {
        var msg = MsgWithSender("Bob");
        var middleware = new AddMessageSenderPrefix<int>(pre: "<", post: ">");
        var context = Context(msg);

        await middleware.Process(context, NoOp);

        Assert.AreEqual("<Bob>", context.Message.Prefix);
    }

    [TestMethod]
    public async Task Process_CustomDelimiters_EmptyStrings_OnlyNameInPrefix()
    {
        var msg = MsgWithSender("Bob");
        var middleware = new AddMessageSenderPrefix<int>(pre: "", post: "");
        var context = Context(msg);

        await middleware.Process(context, NoOp);

        Assert.AreEqual("Bob", context.Message.Prefix);
    }

    // -------------------------------------------------------------------------
    // next is always called
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_WithSender_StillCallsNext()
    {
        var msg = MsgWithSender("Martin");
        var middleware = new AddMessageSenderPrefix<int>();
        var context = Context(msg);
        var called = false;

        await middleware.Process(context, _ => { called = true; return Task.CompletedTask; });

        Assert.IsTrue(called);
    }

    [TestMethod]
    public async Task Process_WithoutSender_StillCallsNext()
    {
        var msg = new Message(MessageRole.User);
        var middleware = new AddMessageSenderPrefix<int>();
        var context = Context(msg);
        var called = false;

        await middleware.Process(context, _ => { called = true; return Task.CompletedTask; });

        Assert.IsTrue(called);
    }

    // -------------------------------------------------------------------------
    // Excluded roles
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Process_ExcludedRole_PrefixUnchanged()
    {
        var msg = MsgWithSender("Martin");
        var middleware = new AddMessageSenderPrefix<int>(exclude: MessageRole.User);
        var context = Context(msg);

        await middleware.Process(context, NoOp);

        Assert.AreEqual("", context.Message.Prefix);
    }

    [TestMethod]
    public async Task Process_ExcludedRole_StillCallsNext()
    {
        var msg = MsgWithSender("Martin");
        var middleware = new AddMessageSenderPrefix<int>(exclude: MessageRole.User);
        var context = Context(msg);
        var called = false;

        await middleware.Process(context, _ => { called = true; return Task.CompletedTask; });

        Assert.IsTrue(called);
    }

    [TestMethod]
    public async Task Process_NonExcludedRole_PrefixApplied()
    {
        var msg = MsgWithSender("Martin");
        var middleware = new AddMessageSenderPrefix<int>(exclude: MessageRole.Assistant);
        var context = Context(msg);

        await middleware.Process(context, NoOp);

        Assert.AreEqual("[Martin]", context.Message.Prefix);
    }

    [TestMethod]
    public async Task Process_ExcludedRoleFlags_MatchingRoleExcluded()
    {
        var msg = MsgWithSender("Martin");
        var middleware = new AddMessageSenderPrefix<int>(exclude: MessageRole.User | MessageRole.Assistant);
        var context = Context(msg);

        await middleware.Process(context, NoOp);

        Assert.AreEqual("", context.Message.Prefix);
    }
}
