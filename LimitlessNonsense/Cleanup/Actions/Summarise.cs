namespace LimitlessNonsense.Cleanup.Actions;

/// <summary>
/// Automatically summarise the context, keeping some of the most ecent messages
/// </summary>
/// <param name="Keep"></param>
internal record Summarise(ushort Keep)
    : ContextAction
{
    public override bool Execute(CleanupContext context)
    {
        throw new NotImplementedException();
    }
}