using LimitlessNonsense.Cleanup.Actions;

namespace LimitlessNonsense.Cleanup;

public sealed record CleanupPolicy(Trigger Trigger, Condition Condition, ContextAction Action)
{
    public bool Execute(ContextState state, List<ContextMessage> messages)
    {
        if (Condition.Evaluate(state))
            return Action.Execute(new CleanupContext(Condition, state, messages));
        return false;
    }
}

public static class IEnumerableOfCleanupPolicyExtensions
{
    /// <summary>
    /// Execute a list of cleanup policies sequentially, stopping if any policy causes a change.
    /// </summary>
    /// <param name="policies"></param>
    /// <param name="state"></param>
    /// <param name="messages"></param>
    /// <returns>A value indicating if any changes were made</returns>
    public static bool Execute(this IEnumerable<CleanupPolicy> policies, ContextState state, List<ContextMessage> messages)
    {
        var changed = false;
        foreach (var policy in policies)
            changed |= policy.Execute(state, messages);
        return changed;
    }
}