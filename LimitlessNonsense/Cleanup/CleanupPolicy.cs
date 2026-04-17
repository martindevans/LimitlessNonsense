using LimitlessNonsense.Cleanup.Actions;

namespace LimitlessNonsense.Cleanup;

public sealed record CleanupPolicy(Trigger Trigger, Condition Condition, ContextAction Action)
{
    public async Task<bool> Execute(ContextState state, List<Message> messages, IServiceProvider? services = null)
    {
        if (Condition.Evaluate(state))
            return await Action.Execute(new CleanupContext(Condition, state, messages, services));
        return false;
    }
}