using System.Text.Json.Serialization;

namespace LimitlessNonsense.Cleanup.Actions;

/// <summary>
/// Modifies the context in some way
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ConditionalRepeat), nameof(ConditionalRepeat))]
[JsonDerivedType(typeof(ConditionalSequence), nameof(ConditionalSequence))]
[JsonDerivedType(typeof(RemoveImportance), nameof(Actions.RemoveImportance))]
[JsonDerivedType(typeof(RemoveOldest), nameof(Actions.RemoveOldest))]
[JsonDerivedType(typeof(RemoveRole), nameof(Actions.RemoveRole))]
[JsonDerivedType(typeof(Summarise), nameof(Actions.Summarise))]
public abstract record ContextAction
{
    #region static factories
    /// <summary>
    /// Keep repeating an action until the condition is false
    /// </summary>
    /// <param name="action"></param>
    /// <param name="maxRepeats"></param>
    /// <returns></returns>
    public static ContextAction Repeat(ContextAction action, uint maxRepeats = 32)
    {
        return new ConditionalRepeat(action, maxRepeats);
    }

    /// <summary>
    /// Run a sequence of actions, terminating the sequence as soon as the condition is false
    /// </summary>
    /// <param name="actions"></param>
    /// <returns></returns>
    public static ContextAction Sequence(ReadOnlySpan<ContextAction> actions)
    {
        return new ConditionalSequence(actions.ToArray());
    }

    /// <summary>
    /// Remove messages at or below the threshold which are buried more than the given depth
    /// </summary>
    /// <param name="threshold"></param>
    /// <param name="depth"></param>
    public static ContextAction ImportanceRemoval(Importance threshold, ushort depth = 0)
    {
        return new RemoveImportance(threshold, depth);
    }

    /// <summary>
    /// Remove the oldest message which is one of the given roles
    /// </summary>
    /// <param name="roles"></param>
    /// <returns></returns>
    public static ContextAction RemoveOldest(MessageRole roles)
    {
        return new RemoveOldest(roles);
    }

    /// <summary>
    /// Remove all messages which have one of the given roles which are buried more than the given depth
    /// </summary>
    /// <param name="roles"></param>
    /// <param name="depth"></param>
    /// <returns></returns>
    public static ContextAction RemoveRole(MessageRole roles, ushort depth = 0)
    {
        return new RemoveRole(roles, depth);
    }

    /// <summary>
    /// Summarise the entire conversation, except for some messages at the end.
    /// </summary>
    /// <param name="keep"></param>
    /// <returns></returns>
    public static ContextAction Summarise(ushort keep = 4)
    {
        return new Summarise(keep);
    }
    #endregion

    /// <summary>
    /// Execute this action on the LLM context
    /// </summary>
    /// <returns>True, if any changes were made</returns>
    public abstract bool Execute(CleanupContext context);
}