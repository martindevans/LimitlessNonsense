using LimitlessNonsense.Cleanup;
using LimitlessNonsense.Cleanup.Actions;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class ConditionalRepeatTests
{
    private static CleanupContext Context(Condition condition, int messageCount, Guid? stateId = null)
    {
        var state = new ContextState(stateId ?? Guid.NewGuid(), 50, 100);
        var messages = Enumerable.Range(0, messageCount)
            .Select(_ => new ContextMessage(MessageRole.User))
            .ToList();
        return new CleanupContext(condition, state, messages);
    }

    // -------------------------------------------------------------------------
    // ContextAction.Repeat / ConditionalRepeat.Execute
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Execute_RunsInnerActionMaxRepeatsTimes()
    {
        // Tests ContextAction.Repeat factory and ConditionalRepeat.Execute with an explicit repeat count
        const int maxRepeats = 3;
        var ctx = Context(Condition.True(), messageCount: 10);

        var changed = ContextAction.Repeat(ContextAction.RemoveOldest(MessageRole.User), maxRepeats).Execute(ctx);

        Assert.IsTrue(changed);
        Assert.HasCount(10 - maxRepeats, ctx.Messages);
    }


    [TestMethod]
    public void Execute_StopsEarlyWhenConditionBecomesFalse()
    {
        // Condition.Changed() returns true on the first call (stateId != Guid.Empty) then false on the second
        var stateId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var ctx = Context(Condition.Changed(), messageCount: 5, stateId: stateId);

        var changed = ContextAction.Repeat(ContextAction.RemoveOldest(MessageRole.User), maxRepeats: 10).Execute(ctx);

        // Condition was true only on the first iteration, so exactly one message is removed
        Assert.IsTrue(changed);
        Assert.HasCount(4, ctx.Messages);
    }

    [TestMethod]
    public void Execute_NeverRunsInnerAction_WhenConditionFalseOrMaxRepeatsIsZero()
    {
        // Always-false condition
        var ctx1 = Context(Condition.False(), messageCount: 5);
        var changed1 = ContextAction.Repeat(ContextAction.RemoveOldest(MessageRole.User), maxRepeats: 10).Execute(ctx1);
        Assert.IsFalse(changed1);
        Assert.HasCount(5, ctx1.Messages);

        // Zero max repeats — loop body is never entered even when condition is always true
        var ctx2 = Context(Condition.True(), messageCount: 5);
        var changed2 = ContextAction.Repeat(ContextAction.RemoveOldest(MessageRole.User), maxRepeats: 0).Execute(ctx2);
        Assert.IsFalse(changed2);
        Assert.HasCount(5, ctx2.Messages);
    }
}
