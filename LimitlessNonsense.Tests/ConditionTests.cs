using LimitlessNonsense.ContextManagement;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class ConditionTests
{
    private static readonly Guid GuidA = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid GuidB = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static ContextState State(ulong tokenCount = 50, ulong contextSize = 100, Guid? id = null)
        => new(id ?? GuidA, tokenCount, contextSize);

    // -------------------------------------------------------------------------
    // ConditionAlways
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Always_ReturnsTrue()
    {
        var condition = Condition.True();

        Assert.IsTrue(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Always_ReturnsTrueRepeatedCalls()
    {
        var condition = Condition.True();

        Assert.IsTrue(condition.Evaluate(State()));
        Assert.IsTrue(condition.Evaluate(State()));
        Assert.IsTrue(condition.Evaluate(State()));
    }

    // -------------------------------------------------------------------------
    // ConditionNever
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Never_ReturnsFalse()
    {
        var condition = Condition.False();

        Assert.IsFalse(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Never_ReturnsFalseRepeatedCalls()
    {
        var condition = Condition.False();

        Assert.IsFalse(condition.Evaluate(State()));
        Assert.IsFalse(condition.Evaluate(State()));
        Assert.IsFalse(condition.Evaluate(State()));
    }

    // -------------------------------------------------------------------------
    // ConditionContextFillFactor
    // -------------------------------------------------------------------------

    [TestMethod]
    public void ContextFillFactor_ReturnsFalse_WhenBelowThreshold()
    {
        var condition = Condition.ContextFillFactor(0.75);

        // 50/100 = 0.5, which is below 0.75
        Assert.IsFalse(condition.Evaluate(State(tokenCount: 50, contextSize: 100)));
    }

    [TestMethod]
    public void ContextFillFactor_ReturnsTrue_WhenAtThreshold()
    {
        var condition = Condition.ContextFillFactor(0.75);

        // 75/100 = 0.75, which equals threshold
        Assert.IsTrue(condition.Evaluate(State(tokenCount: 75, contextSize: 100)));
    }

    [TestMethod]
    public void ContextFillFactor_ReturnsTrue_WhenAboveThreshold()
    {
        var condition = Condition.ContextFillFactor(0.75);

        // 90/100 = 0.9, which is above 0.75
        Assert.IsTrue(condition.Evaluate(State(tokenCount: 90, contextSize: 100)));
    }

    [TestMethod]
    public void ContextFillFactor_ReturnsTrue_WhenFullyFilled()
    {
        var condition = Condition.ContextFillFactor(0.5);

        // 100/100 = 1.0, above threshold
        Assert.IsTrue(condition.Evaluate(State(tokenCount: 100, contextSize: 100)));
    }

    [TestMethod]
    public void ContextFillFactor_ZeroFactor_ReturnsTrueForNonEmptyContext()
    {
        var condition = Condition.ContextFillFactor(0.0);

        // Any non-zero fill satisfies factor=0
        Assert.IsTrue(condition.Evaluate(State(tokenCount: 1, contextSize: 100)));
    }

    [TestMethod]
    public void ContextFillFactor_ZeroTokens_ReturnsFalse()
    {
        var condition = Condition.ContextFillFactor(0.5);

        // 0/100 = 0, below 0.5
        Assert.IsFalse(condition.Evaluate(State(tokenCount: 0, contextSize: 100)));
    }

    [TestMethod]
    public void ContextFillFactor_ThrowsForNegativeFactor()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Condition.ContextFillFactor(-0.1));
    }

    [TestMethod]
    public void ContextFillFactor_ThrowsForFactorOfOne()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Condition.ContextFillFactor(1.0));
    }

    [TestMethod]
    public void ContextFillFactor_ThrowsForFactorGreaterThanOne()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Condition.ContextFillFactor(1.5));
    }

    [TestMethod]
    public void ContextFillFactor_ZeroContextSize_ZeroTokens_ReturnsFalse()
    {
        // (double)0 / 0 = NaN, and NaN >= factor is always false
        var condition = Condition.ContextFillFactor(0.5);

        Assert.IsFalse(condition.Evaluate(State(tokenCount: 0, contextSize: 0)));
    }

    [TestMethod]
    public void ContextFillFactor_ZeroContextSize_NonZeroTokens_ReturnsTrue()
    {
        // (double)1 / 0 = +Infinity, which is >= any valid factor
        var condition = Condition.ContextFillFactor(0.5);

        Assert.IsTrue(condition.Evaluate(State(tokenCount: 1, contextSize: 0)));
    }

    // -------------------------------------------------------------------------
    // ConditionChanged
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Changed_ReturnsTrueOnFirstCall_WhenIdDiffersFromDefault()
    {
        // Initial State is Guid.Empty; any non-empty ID is "changed"
        var condition = Condition.Changed();

        Assert.IsTrue(condition.Evaluate(State(id: GuidA)));
    }

    [TestMethod]
    public void Changed_ReturnsFalseOnFirstCall_WhenIdIsDefault()
    {
        // Initial State is Guid.Empty; Guid.Empty ID is not "changed"
        var condition = Condition.Changed();

        Assert.IsFalse(condition.Evaluate(State(id: Guid.Empty)));
    }

    [TestMethod]
    public void Changed_ReturnsFalseOnRepeatedCallWithSameId()
    {
        var condition = Condition.Changed();

        condition.Evaluate(State(id: GuidA)); // first call — sets State to GuidA
        Assert.IsFalse(condition.Evaluate(State(id: GuidA)));
    }

    [TestMethod]
    public void Changed_ReturnsTrueWhenIdChanges()
    {
        var condition = Condition.Changed();

        condition.Evaluate(State(id: GuidA)); // set State to GuidA
        Assert.IsTrue(condition.Evaluate(State(id: GuidB)));
    }

    [TestMethod]
    public void Changed_UpdatesStateAfterEachCall()
    {
        var condition = Condition.Changed();

        condition.Evaluate(State(id: GuidA)); // GuidA
        condition.Evaluate(State(id: GuidB)); // GuidB — returns true (changed)
        // Now state should be GuidB, so same ID returns false
        Assert.IsFalse(condition.Evaluate(State(id: GuidB)));
    }

    // -------------------------------------------------------------------------
    // Operator & (ConditionAnd)
    // -------------------------------------------------------------------------

    [TestMethod]
    public void And_TrueAndTrue_ReturnsTrue()
    {
        var condition = Condition.True() & Condition.True();

        Assert.IsTrue(condition.Evaluate(State()));
    }

    [TestMethod]
    public void And_TrueAndFalse_ReturnsFalse()
    {
        var condition = Condition.True() & Condition.False();

        Assert.IsFalse(condition.Evaluate(State()));
    }

    [TestMethod]
    public void And_FalseAndTrue_ReturnsFalse()
    {
        var condition = Condition.False() & Condition.True();

        Assert.IsFalse(condition.Evaluate(State()));
    }

    [TestMethod]
    public void And_FalseAndFalse_ReturnsFalse()
    {
        var condition = Condition.False() & Condition.False();

        Assert.IsFalse(condition.Evaluate(State()));
    }

    [TestMethod]
    public void And_EvaluatesBothSides()
    {
        // ConditionChanged tracks state; verify both sides are evaluated even when
        // the first side is false (bitwise &, not short-circuit &&)
        var changedA = Condition.Changed();
        var changedB = Condition.Changed();
        var condition = Condition.False() & (changedA & changedB);

        // With short-circuit logic, changedA and changedB would NOT be evaluated.
        // With bitwise &, all sides ARE evaluated.
        condition.Evaluate(State(id: GuidA));

        // If both were evaluated, their states should now be GuidA, so same ID = false
        Assert.IsFalse(changedA.Evaluate(State(id: GuidA)));
        Assert.IsFalse(changedB.Evaluate(State(id: GuidA)));
    }

    [TestMethod]
    public void And_OperatorCreatesConditionAnd()
    {
        var condition = Condition.True() & Condition.False();

        Assert.IsInstanceOfType<ConditionAnd>(condition);
    }

    // -------------------------------------------------------------------------
    // Operator | (ConditionOr)
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Or_TrueOrTrue_ReturnsTrue()
    {
        var condition = Condition.True() | Condition.True();

        Assert.IsTrue(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Or_TrueOrFalse_ReturnsTrue()
    {
        var condition = Condition.True() | Condition.False();

        Assert.IsTrue(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Or_FalseOrTrue_ReturnsTrue()
    {
        var condition = Condition.False() | Condition.True();

        Assert.IsTrue(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Or_FalseOrFalse_ReturnsFalse()
    {
        var condition = Condition.False() | Condition.False();

        Assert.IsFalse(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Or_EvaluatesBothSides()
    {
        // Verify bitwise | evaluates both sides even when first is true
        var changedA = Condition.Changed();
        var changedB = Condition.Changed();
        var condition = Condition.True() | (changedA & changedB);

        condition.Evaluate(State(id: GuidA));

        Assert.IsFalse(changedA.Evaluate(State(id: GuidA)));
        Assert.IsFalse(changedB.Evaluate(State(id: GuidA)));
    }

    [TestMethod]
    public void Or_OperatorCreatesConditionOr()
    {
        var condition = Condition.True() | Condition.False();

        Assert.IsInstanceOfType<ConditionOr>(condition);
    }

    // -------------------------------------------------------------------------
    // Operator ^ (ConditionXor)
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Xor_TrueXorTrue_ReturnsFalse()
    {
        var condition = Condition.True() ^ Condition.True();

        Assert.IsFalse(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Xor_TrueXorFalse_ReturnsTrue()
    {
        var condition = Condition.True() ^ Condition.False();

        Assert.IsTrue(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Xor_FalseXorTrue_ReturnsTrue()
    {
        var condition = Condition.False() ^ Condition.True();

        Assert.IsTrue(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Xor_FalseXorFalse_ReturnsFalse()
    {
        var condition = Condition.False() ^ Condition.False();

        Assert.IsFalse(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Xor_OperatorCreatesConditionXor()
    {
        var condition = Condition.True() ^ Condition.False();

        Assert.IsInstanceOfType<ConditionXor>(condition);
    }

    // -------------------------------------------------------------------------
    // Operator ! (ConditionNot)
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Not_InvertsTrue_ReturnsFalse()
    {
        var condition = !Condition.True();

        Assert.IsFalse(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Not_InvertsFalse_ReturnsTrue()
    {
        var condition = !Condition.False();

        Assert.IsTrue(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Not_OperatorCreatesConditionNot()
    {
        var condition = !Condition.True();

        Assert.IsInstanceOfType<ConditionNot>(condition);
    }

    // -------------------------------------------------------------------------
    // Composition
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Composition_NotAlways_ReturnsFalse()
    {
        // !Always = false
        var condition = !Condition.True();

        Assert.IsFalse(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Composition_NotNever_ReturnsTrue()
    {
        // !Never = true
        var condition = !Condition.False();

        Assert.IsTrue(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Composition_AlwaysAndNotNever_ReturnsTrue()
    {
        // Always & !Never = true & true = true
        var condition = Condition.True() & !Condition.False();

        Assert.IsTrue(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Composition_NeverOrAlways_ReturnsTrue()
    {
        // Never | Always = false | true = true
        var condition = Condition.False() | Condition.True();

        Assert.IsTrue(condition.Evaluate(State()));
    }

    [TestMethod]
    public void Composition_FillFactorAndChanged_ReturnsTrueWhenBothMet()
    {
        var condition = Condition.ContextFillFactor(0.75) & Condition.Changed();

        // 80/100 = 0.8 >= 0.75, and GuidA != Guid.Empty (initial state)
        Assert.IsTrue(condition.Evaluate(State(tokenCount: 80, contextSize: 100, id: GuidA)));
    }

    [TestMethod]
    public void Composition_FillFactorAndChanged_ReturnsFalseWhenFillNotMet()
    {
        var condition = Condition.ContextFillFactor(0.75) & Condition.Changed();

        // 50/100 = 0.5 < 0.75, so AND result is false regardless of Changed
        Assert.IsFalse(condition.Evaluate(State(tokenCount: 50, contextSize: 100, id: GuidA)));
    }
}
