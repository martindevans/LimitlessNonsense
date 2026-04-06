namespace LimitlessNonsense.Cleanup.Actions;

/// <summary>
/// The the oldest message which has any of the given roles
/// </summary>
/// <param name="Roles"></param>
internal record RemoveOldest(MessageRole Roles)
    : ContextAction
{
    public override bool Execute(CleanupContext context)
    {
        for (var i = 0; i < context.Messages.Count; i++)
        {
            var msg = context.Messages[i];
            if ((msg.Role & Roles) != 0)
            {
                context.Messages.RemoveAt(i);
                return true;
            }
        }

        return false;
    }
}