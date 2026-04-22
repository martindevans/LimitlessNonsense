namespace LimitlessNonsense.Middleware;

/// <summary>
/// Represents the context passed to a middleware invocation, including the existing message history,
/// the current UTC timestamp, the message being processed, and any caller-supplied user data associated
/// with the current operation.
/// </summary>
/// <typeparam name="TUserData">
/// The type of caller-provided state carried with this context. This value is supplied by the code creating
/// the <see cref="MiddlewareContext{TUserData}"/> and can be used to pass per-operation data through the
/// middleware pipeline. Its lifetime and ownership are determined by the caller, and any thread-safety
/// guarantees depend on the concrete type and how that instance is shared.
/// </typeparam>
/// <param name="History">History of all messages.</param>
/// <param name="UtcNow">The current UTC time for this middleware invocation.</param>
/// <param name="Message">The new message that is about to be added.</param>
/// <param name="UserData">Caller-provided user data associated with the current middleware operation.</param>
public sealed record MiddlewareContext<TUserData>(
    List<Message> History,
    DateTime UtcNow,
    Message Message,
    TUserData UserData
);