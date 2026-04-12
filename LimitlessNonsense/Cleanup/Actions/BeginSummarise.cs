namespace LimitlessNonsense.Cleanup.Actions;

/// <summary>
/// Begin automatically summarising the context, keeping some of the most recent messages at the end.
/// The summarisation task is stored into a slot.
/// </summary>
/// <param name="Keep"></param>
internal record BeginSummarise(SummarySlot Slot, ushort Keep)
    : ContextAction
{
    public override bool Execute(CleanupContext context)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Wait on a summary task and swap it into the context.
/// </summary>
/// <param name="Slot"></param>
internal record EndSummarise(SummarySlot Slot, bool Wait)
    : ContextAction
{
    public override bool Execute(CleanupContext context)
    {
        throw new NotImplementedException();
    }
}

public class SummarySlot
{
    
}