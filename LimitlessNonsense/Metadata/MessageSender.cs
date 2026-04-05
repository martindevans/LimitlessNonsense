namespace LimitlessNonsense.Metadata;

/// <summary>
/// Metadata indicating the name of the sender of this message
/// </summary>
/// <param name="Name"></param>
public sealed record MessageSender(string Name)
    : IMessageMetadata;