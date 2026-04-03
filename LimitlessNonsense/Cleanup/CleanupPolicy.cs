using LimitlessNonsense.Cleanup.Actions;

namespace LimitlessNonsense.Cleanup;

public sealed record CleanupPolicy(Trigger Trigger, Condition Condition, ContextAction Action);