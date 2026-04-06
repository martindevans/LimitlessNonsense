namespace LimitlessNonsense.Cleanup.Actions;

/// <summary>
/// Remove messages with the given role that are buried over a certain depth
/// </summary>
/// <param name="Roles"></param>
/// <param name="Depth"></param>
internal record RemoveRole(MessageRole Roles, ushort Depth)
    : ContextAction
{
    public override bool Execute(CleanupContext context)
    {
        var changed = false;
        
        for (var i = context.Messages.Count - 1 - Depth; i >= 0; i--)
        {
            var msg = context.Messages[i];

            if ((msg.Role & Roles) != 0)
            {
                context.Messages.RemoveAt(i);
                changed = true;
            }
        }

        return changed;
    }
}