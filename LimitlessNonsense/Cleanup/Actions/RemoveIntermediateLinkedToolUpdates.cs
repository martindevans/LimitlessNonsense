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
        var isRemoving = new HashSet<Guid>();

        // Iterate backwards
        for (var i = context.Messages.Count - 1; i >= 0; i--)
        {
            var msg = context.Messages[i];

            // Skip messages that don't have the metadata
            var link = msg.TryGetMetadata<LinkedToolCall>();
            if (link == null)
                continue;

            // We found a message from this ID, we want to remove all earlier update messages.
            if (!isRemoving.Add(link.Id))
            {
                // Remove this message
                if (link.Type == LinkedToolCallType.Update)
                {
                    context.Messages.RemoveAt(i);
                    changed = true;
                }
            }
        }

        return changed;
    }
}