using System.Text.Json.Serialization;

namespace LimitlessNonsense.Cleanup.Actions;

/// <summary>
/// Remove messages at or below the given importance threshold which are buried over a certain depth
/// </summary>
/// <param name="Threshold"></param>
/// <param name="Depth"></param>
internal record RemoveImportance(MessageImportance Threshold, ushort Depth = 0)
    : ContextAction
{
    public override async Task<bool> Execute(CleanupContext context)
    {
        var changed = false;
        
        for (var i = context.Messages.Count - 1 - Depth; i >= 0; i--)
        {
            var msg = context.Messages[i];
            if (msg.Importance <= Threshold)
            {
                context.Messages.RemoveAt(i);
                changed = true;
            }
        }

        return changed;
    }
}