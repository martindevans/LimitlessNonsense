namespace LimitlessNonsense.Metadata;

public sealed record LinkedToolCall(Guid Id, LinkedToolCallType Type)
    : IMessageMetadata;

public enum LinkedToolCallType
{
    /// <summary>
    /// Initial tool call
    /// </summary>
    Call,
    
    /// <summary>
    /// An update message related to the original
    /// </summary>
    Update,
    
    /// <summary>
    /// The final result of the tool call
    /// </summary>
    Result,
}