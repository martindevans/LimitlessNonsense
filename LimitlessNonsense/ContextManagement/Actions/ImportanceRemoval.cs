namespace LimitlessNonsense.ContextManagement.Actions;

internal record ImportanceRemoval(Importance Threshold, uint Depth = 0)
    : ContextAction
{
    
}