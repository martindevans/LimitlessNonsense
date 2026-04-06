using System.Text.Json;
using LimitlessNonsense.Cleanup;
using LimitlessNonsense.Cleanup.Actions;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class ConditionalSequenceTests
{
    private sealed record TrackingAction : ContextAction
    {
        private readonly List<int> _order;
        private readonly int _id;
        public int ExecuteCount { get; private set; }

        public TrackingAction(List<int> order, int id)
        {
            _order = order;
            _id = id;
        }

        public override bool Execute(CleanupContext context)
        {
            ExecuteCount++;
            _order.Add(_id);
            return true;
        }
    }

    private static CleanupContext CreateContext(params ContextMessage[] messages)
    {
        return new CleanupContext(
            Condition.True(),
            new ContextState(Guid.NewGuid(), 50, 100),
            messages.ToList()
        );
    }

    // -------------------------------------------------------------------------
    // Execute
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Execute_RunsAllActionsInOrder()
    {
        var order = new List<int>();
        var a1 = new TrackingAction(order, 1);
        var a2 = new TrackingAction(order, 2);
        var a3 = new TrackingAction(order, 3);

        var sequence = ContextAction.Sequence([a1, a2, a3]);
        var changed = sequence.Execute(CreateContext());

        Assert.IsTrue(changed);
        Assert.AreEqual(1, a1.ExecuteCount);
        Assert.AreEqual(1, a2.ExecuteCount);
        Assert.AreEqual(1, a3.ExecuteCount);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, order);
    }

    [TestMethod]
    public void Execute_EmptySequence_DoesNothing()
    {
        var sequence = ContextAction.Sequence([]);
        var changed = sequence.Execute(CreateContext()); // Must not throw

        Assert.IsFalse(changed);
    }

    [TestMethod]
    public void Execute_ContextModificationsPropagate_BetweenActions()
    {
        var msg1 = new ContextMessage(MessageRole.User);
        var msg2 = new ContextMessage(MessageRole.User);
        var ctx = CreateContext(msg1, msg2);

        // Two sequential removals: the second should see the result of the first
        var sequence = ContextAction.Sequence([
            ContextAction.RemoveOldest(MessageRole.User),
            ContextAction.RemoveOldest(MessageRole.User),
        ]);
        var changed = sequence.Execute(ctx);

        Assert.IsTrue(changed);
        Assert.IsEmpty(ctx.Messages);
    }

    // -------------------------------------------------------------------------
    // JSON Serialization
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Sequence_RoundTrips_ThroughJson()
    {
        var sequence = ContextAction.Sequence([ContextAction.RemoveOldest(MessageRole.User)]);

        var json = JsonSerializer.Serialize(sequence);
        var deserialized = JsonSerializer.Deserialize<ContextAction>(json);
        var rejson = JsonSerializer.Serialize(deserialized);

        Assert.AreEqual(json, rejson);
    }
}
