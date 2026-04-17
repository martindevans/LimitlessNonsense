using LimitlessNonsense.Metadata;

namespace LimitlessNonsense.Cleanup.Actions;

/// <summary>
/// Handles messages tagged with <see cref="LinkedToolCall"/> metadata. If we assume the first call is important (actual query) and the final
/// one is important (the result) but intermediate ones are just temporary updates on progress, so only the latest update is important.
///
/// If there's a final result remove all updates, otherwise remove all but the final update
/// </summary>
internal record RemoveIntermediateLinkedToolUpdates
    : ContextAction
{
    public override async Task<bool> Execute(CleanupContext context)
    {
        var changed = false;
        var isRemoving = new Dictionary<Guid, bool>();

        // Iterate backwards
        for (var i = context.Messages.Count - 1; i >= 0; i--)
        {
            var msg = context.Messages[i];

            // Skip messages that don't have the metadata
            var link = msg.TryGetMetadata<LinkedToolCall>();
            if (link == null)
                continue;

            // If we don't know state, update it now
            if (!isRemoving.TryGetValue(link.Id, out var removing))
            {
                // We found a message, whatever it is remove earlier updates
                isRemoving[link.Id] = true;
            }
            else
            {
                // Remove this message
                if (removing && link.Type == LinkedToolCallType.Update)
                {
                    context.Messages.RemoveAt(i);
                    changed = true;
                }
            }
        }

        return changed;
    }
}