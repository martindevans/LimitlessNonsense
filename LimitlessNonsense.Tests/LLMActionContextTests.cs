using LimitlessNonsense.Cleanup;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class CleanupContextTests
{
    private static readonly Guid GuidA = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid GuidB = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static ContextState DefaultState()
        => new(GuidA, TokenCount: 50, ContextSize: 100);

    

    private static ContextMessage Msg(Guid id, MessageRole role = MessageRole.User, Importance importance = Importance.Normal)
        => new(role, guid: id) { Importance = importance };

    // -------------------------------------------------------------------------
    // Construction
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Constructor_SetsAllProperties()
    {
        var condition = Condition.True();
        var state = DefaultState();
        List<ContextMessage> messages = [Msg(GuidA), Msg(GuidB)];

        var ctx = new CleanupContext(condition, state, messages);

        Assert.AreEqual(condition, ctx.Condition);
        Assert.AreEqual(state, ctx.State);
        CollectionAssert.AreEqual(messages.ToList(), ctx.Messages.ToList());
    }
}
