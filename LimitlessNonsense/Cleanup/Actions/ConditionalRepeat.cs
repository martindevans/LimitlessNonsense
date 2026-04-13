namespace LimitlessNonsense.Cleanup.Actions;

/// <summary>
/// Repeat an action until the condition is satisfied
/// </summary>
internal record ConditionalRepeat(ContextAction Action, uint MaxRepeats = 32)
    : ContextAction
{
    public override async Task<bool> Execute(CleanupContext context)
    {
        var changed = false;
        
        for (var i = 0; i < MaxRepeats; i++)
        {
            if (!context.Condition.Evaluate(context.State))
                return changed;

            changed |= await Action.Execute(context);
        }

        return changed;
    }
}