using LimitlessNonsense.Cleanup.Actions;

namespace LimitlessNonsense.Cleanup;

public sealed record CleanupPolicy(Trigger Trigger, Condition Condition, ContextAction Action)
{
    public bool Execute(ContextState state, List<ContextMessage> messages, IServiceProvider? services = null)
    {
        if (Condition.Evaluate(state))
            return Action.Execute(new CleanupContext(Condition, state, messages, services));
        return false;
    }
}