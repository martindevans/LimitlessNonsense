namespace LimitlessNonsense.Cleanup.Actions;

/// <summary>
/// Run a sequence of actions, stopping as soon as the condition is satisfied
/// </summary>
internal record ConditionalSequence(IReadOnlyList<ContextAction> Actions)
    : ContextAction
{
    public override void Execute(CleanupContext context)
    {
        for (var i = 0; i < Actions.Count; i++)
            Actions[i].Execute(context);
    }
}