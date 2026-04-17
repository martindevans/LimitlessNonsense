using LimitlessNonsense.Cleanup.Actions;

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
    public List<Message> Messages { get; private set; }

    /// <summary>Service provider</summary>
    public IServiceProvider? Services { get; init; }

    /// <summary>The currently in-flight summarisation task, if any</summary>
    public SummarisationTask? ActiveSummarisationTask { get; set; }

    /// <summary>
    /// LLM context handle used for applying policy changes
    /// </summary>
    /// <param name="condition">Condition that triggered this action</param>
    /// <param name="state">State of the context</param>
    /// <param name="messages">List of messages in the context</param>
    /// <param name="services"></param>
    public CleanupContext(Condition condition, ContextState state, List<Message> messages, IServiceProvider? services = null)
    {
        Condition = condition;
        State = state;

        Messages = messages;
        Services = services;
    }
}
