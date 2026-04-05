namespace LimitlessNonsense.Metadata;

/// <summary>
/// Metadata indicating when this message was created
/// </summary>
/// <param name="Time"></param>
public sealed record MessageCreationTime(DateTime Time)
    : IMessageMetadata;