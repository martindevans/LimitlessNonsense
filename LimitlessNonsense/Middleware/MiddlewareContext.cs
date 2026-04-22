namespace LimitlessNonsense.Middleware;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TUserData"></typeparam>
/// <param name="History">History of all messages</param>
/// <param name="UtcNow">The current time</param>
/// <param name="Message">The new message that is about to be added</param>
/// <param name="UserData">Extra user data</param>
public sealed record MiddlewareContext<TUserData>(
    List<Message> History,
    DateTime UtcNow,
    Message Message,
    TUserData UserData
);