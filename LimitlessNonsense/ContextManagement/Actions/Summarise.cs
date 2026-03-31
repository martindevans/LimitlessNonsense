namespace LimitlessNonsense.ContextManagement.Actions;

/// <summary>
/// Automatically summarise the context, keeping some of the most ecent messages
/// </summary>
/// <param name="Keep"></param>
internal record Summarise(ushort Keep)
    : ContextAction
{
    public override void Execute(LLMActionContext context)
    {
        throw new NotImplementedException();
    }
}