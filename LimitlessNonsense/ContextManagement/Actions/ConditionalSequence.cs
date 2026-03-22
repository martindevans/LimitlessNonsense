namespace LimitlessNonsense.ContextManagement.Actions;

/// <summary>
/// Run a sequence of actions, stopping as soon as the condition is satisfied
/// </summary>
internal record ConditionalSequence(IReadOnlyList<ContextAction> Actions)
    : ContextAction
{
}