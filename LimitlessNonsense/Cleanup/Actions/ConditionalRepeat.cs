namespace LimitlessNonsense.Cleanup.Actions;

/// <summary>
/// Repeat an action until the condition is satisfied
/// </summary>
internal record ConditionalRepeat(ContextAction Action, uint MaxRepeats = 32)
    : ContextAction
{
    public override void Execute(CleanupContext context)
    {
        for (var i = 0; i < MaxRepeats; i++)
        {
            if (!context.Condition.Evaluate(context.State))
                return;

            Action.Execute(context);
        }
    }
}