namespace LimitlessNonsense.Cleanup;

/// <summary>
/// LLM context handle used for applying policy changes
/// </summary>
public record CleanupContext
{
    /// <summary>Condition that triggered this action</summary>
    public Condition Condition { get; init; }

    /// <summary>State of the context</summary>
    public ContextState State { get; init; }

    /// <summary>List of messages in the context</summary>
    public List<ContextMessage> Messages { get; private set; }

    /// <summary>
    /// LLM context handle used for applying policy changes
    /// </summary>
    /// <param name="condition">Condition that triggered this action</param>
    /// <param name="state">State of the context</param>
    /// <param name="messages">List of messages in the context</param>
    public CleanupContext(Condition condition, ContextState state, List<ContextMessage> messages)
    {
        Condition = condition;
        State = state;

        Messages = messages;
    }
}
