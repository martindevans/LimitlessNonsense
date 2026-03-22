namespace LimitlessNonsense.ContextManagement.Actions;

internal record RemoveRole(MessageRole Role, uint Depth)
    : ContextAction
{
}