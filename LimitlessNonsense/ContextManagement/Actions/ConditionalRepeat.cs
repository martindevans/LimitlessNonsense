namespace LimitlessNonsense.ContextManagement.Actions;

/// <summary>
/// Repeat an action until the condition is satisfied
/// </summary>
internal record ConditionalRepeat(ContextAction Action, uint MaxRepeats = 32)
    : ContextAction
{
}