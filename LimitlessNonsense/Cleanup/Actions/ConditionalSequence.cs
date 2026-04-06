namespace LimitlessNonsense.Cleanup.Actions;

/// <summary>
/// Run a sequence of actions, stopping as soon as the condition is satisfied
/// </summary>
internal record ConditionalSequence(IReadOnlyList<ContextAction> Actions)
    : ContextAction
{
    public override bool Execute(CleanupContext context)
    {
        var changed = false;

        for (var i = 0; i < Actions.Count; i++)
        {
            if (!context.Condition.Evaluate(context.State))
                break;
            
            changed |= Actions[i].Execute(context);
        }

        return changed;
    }
}