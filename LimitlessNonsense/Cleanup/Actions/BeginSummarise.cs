namespace LimitlessNonsense.Cleanup.Actions;

/// <summary>
/// Begin automatically summarising the context, keeping some of the most recent messages at the end.
/// </summary>
/// <param name="Keep"></param>
internal record BeginSummarise(ushort Keep)
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
/// <param name="Block"></param>
internal record EndSummarise(bool Block)
    : ContextAction
{
    public override bool Execute(CleanupContext context)
    {
        throw new NotImplementedException();
    }
}