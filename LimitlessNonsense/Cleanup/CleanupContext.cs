namespace LimitlessNonsense.Cleanup;

/// <summary>
/// LLM context handle used for applying policy changes
/// </summary>
public record CleanupContext
{
    private readonly HashSet<Guid> _removals = [];
    internal IEnumerable<Guid> Removals => _removals;

    private readonly List<ContextMessage> _messages;

    /// <summary>Condition that triggered this action</summary>
    public Condition Condition { get; init; }

    /// <summary>State of the context</summary>
    public ContextState State { get; init; }

    /// <summary>List of messages in the context</summary>
    public IReadOnlyList<ContextMessage> Messages => _messages;

    /// <summary>
    /// LLM context handle used for applying policy changes
    /// </summary>
    /// <param name="condition">Condition that triggered this action</param>
    /// <param name="state">State of the context</param>
    /// <param name="messages">List of messages in the context</param>
    public CleanupContext(Condition condition, ContextState state, IReadOnlyList<ContextMessage> messages)
    {
        Condition = condition;
        State = state;

        _messages = messages.ToList();
    }

    /// <summary>
    /// Remove the given message
    /// </summary>
    /// <param name="msg"></param>
    public void Remove(ContextMessage msg)
    {
        _removals.Add(msg.ID);
        _messages.Remove(msg);
    }
}
