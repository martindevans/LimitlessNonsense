using System.Text.Json.Serialization;

namespace LimitlessNonsense.ContextManagement;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ConditionAlways), nameof(ConditionAlways))]
[JsonDerivedType(typeof(ConditionNever), nameof(ConditionNever))]
[JsonDerivedType(typeof(ConditionContextFillFactor), nameof(ConditionContextFillFactor))]
[JsonDerivedType(typeof(ConditionChanged), nameof(ConditionChanged))]
[JsonDerivedType(typeof(ConditionAnd), nameof(ConditionAnd))]
[JsonDerivedType(typeof(ConditionOr), nameof(ConditionOr))]
[JsonDerivedType(typeof(ConditionXor), nameof(ConditionXor))]
[JsonDerivedType(typeof(ConditionNot), nameof(ConditionNot))]
public abstract record Condition
{
    #region static factories
    /// <summary>
    /// Always evaluate to true
    /// </summary>
    /// <returns></returns>
    public static Condition True() => new ConditionAlways();

    /// <summary>
    /// Always evaluate to false
    /// </summary>
    /// <returns></returns>
    public static Condition False() => new ConditionNever();

    /// <summary>
    /// Evaluate if the context fill factor is greater than or equal to the given threshold (0 to 1)
    /// </summary>
    /// <param name="factor"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static Condition ContextFillFactor(double factor)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(factor, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(factor, 1);

        return new ConditionContextFillFactor(factor);
    }

    /// <summary>
    /// Evaluate true if the input has changed since the last check
    /// </summary>
    /// <returns></returns>
    public static Condition Changed()
    {
        return new ConditionChanged();
    }

    /// <summary>
    /// Evaluate true if both internal conditions are true
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Condition operator &(Condition a, Condition b)
    {
        return new ConditionAnd(a, b);
    }

    /// <summary>
    /// Evaluate true if either internal condition is true
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Condition operator |(Condition a, Condition b)
    {
        return new ConditionOr(a, b);
    }

    /// <summary>
    /// Evaluate true if either internal condition is true (but not both)
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Condition operator ^(Condition a, Condition b)
    {
        return new ConditionXor(a, b);
    }

    /// <summary>
    /// Invert another condition
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    public static Condition operator !(Condition a)
    {
        return new ConditionNot(a);
    }
    #endregion

    protected internal abstract bool Evaluate(ContextState state);
}

/// <summary>
/// State of the LLM context
/// </summary>
/// <param name="ID"></param>
/// <param name="TokenCount"></param>
/// <param name="ContextSize"></param>
public record ContextState(Guid ID, ulong TokenCount, ulong ContextSize);

internal record ConditionAlways
    : Condition
{
    protected internal override bool Evaluate(ContextState state)
    {
        return true;
    }
}

internal record ConditionNever
    : Condition
{
    protected internal override bool Evaluate(ContextState state)
    {
        return false;
    }
}

public record ConditionChanged
    : Condition
{
    public Guid State { get; set; }

    protected internal override bool Evaluate(ContextState state)
    {
        var changed = state.ID != State;
        State = state.ID;
        return changed;
    }
}

public record ConditionAnd(Condition A, Condition B)
    : Condition
{
    protected internal override bool Evaluate(ContextState state)
    {
        return A.Evaluate(state)
             & B.Evaluate(state);
    }
}

public record ConditionOr(Condition A, Condition B)
    : Condition
{
    protected internal override bool Evaluate(ContextState state)
    {
        return A.Evaluate(state)
             | B.Evaluate(state);
    }
}

public record ConditionXor(Condition A, Condition B)
    : Condition
{
    protected internal override bool Evaluate(ContextState state)
    {
        return A.Evaluate(state)
             ^ B.Evaluate(state);
    }
}

public record ConditionNot(Condition A)
    : Condition
{
    protected internal override bool Evaluate(ContextState state)
    {
        return !A.Evaluate(state);
    }
}

internal record ConditionContextFillFactor(double Factor)
    : Condition
{
    protected internal override bool Evaluate(ContextState state)
    {
        var fill = (double)state.TokenCount / state.ContextSize;
        return fill >= Factor;
    }
}