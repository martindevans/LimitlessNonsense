using LimitlessNonsense.ContextManagement.Actions;

namespace LimitlessNonsense.ContextManagement;

public sealed record Policy(Trigger Trigger, Condition Condition, ContextAction Action);