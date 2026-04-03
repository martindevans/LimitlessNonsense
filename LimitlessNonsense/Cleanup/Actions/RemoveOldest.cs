namespace LimitlessNonsense.Cleanup.Actions;

/// <summary>
/// The the oldest message which has any of the given roles
/// </summary>
/// <param name="Roles"></param>
internal record RemoveOldest(MessageRole Roles)
    : ContextAction
{
    public override void Execute(CleanupContext context)
    {
        for (var i = 0; i < context.Messages.Count; i++)
        {
            var msg = context.Messages[i];
            if ((msg.Role & Roles) != 0)
            {
                context.Remove(msg);
                break;
            }
        }
    }
}