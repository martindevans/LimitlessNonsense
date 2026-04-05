//using System.Text.Json;

//namespace LimitlessNonsense
//{
//    /// <summary>
//    /// Core context for interaction with LLM
//    /// </summary>
//    public class ConversationalContext<TMessage>
//        where TMessage : class, IMessage
//    {
//        public string Model { get; }

//        public ConversationalContext(string model)
//        {
//            Model = model;
//        }

//        #region save/load
//        /// <summary>
//        /// Save the messages to a JSON string
//        /// </summary>
//        /// <returns></returns>
//        public string Save()
//        {
//            var json = JsonSerializer.Serialize(_messages);
//            return json;
//        }

//        /// <summary>
//        /// Load messages from the JSON string
//        /// </summary>
//        /// <param name="json"></param>
//        /// <param name="overwriteSystemPrompt"></param>
//        public void Load(string json, bool overwriteSystemPrompt = false)
//        {
//            // Get the system prompt
//            var sys = _messages.Count > 0 ? _messages[0] : null;

//            // Remove all messages
//            _messages.Clear();

//            // Load messages
//            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? [];
//            _messages.AddRange(messages);

//            // Restore system prompt
//            if (sys != null && !overwriteSystemPrompt)
//                ReplaceSystemPrompt(sys.GetMessageContent());
//        }
//        #endregion

//        private class Turn
//        {
//            /// <summary>
//            /// Generic metadata attached to this turn
//            /// </summary>
//            private readonly Dictionary<Type, object?> _metadata = new();

//            /// <summary>
//            /// The actual message
//            /// </summary>
//            public TMessage Message { get; }

//            public MessageRole Role => Message.Role;

//            public Turn(TMessage message)
//            {
//                Message = message;
//            }

//            /// <summary>
//            /// Try to get metadata attached to this turn
//            /// </summary>
//            /// <typeparam name="T"></typeparam>
//            /// <returns></returns>
//            public T? GetMetadata<T>(T? @default = default)
//            {
//                if (_metadata.TryGetValue(typeof(T), out var value))
//                    return (T?)value;
//                return @default;
//            }

//            /// <summary>
//            /// Add new metadata, throws if the metadata is already present
//            /// </summary>
//            /// <typeparam name="T"></typeparam>
//            /// <param name="data"></param>
//            public void AddMetadata<T>(T data)
//            {
//                if (_metadata.ContainsKey(typeof(T)))
//                    throw new InvalidOperationException($"Turn already has '{typeof(T)}' metadata attached");
//            }

//            /// <summary>
//            /// Set metadata, overwrites if the metadata is already present
//            /// </summary>
//            /// <typeparam name="T"></typeparam>
//            /// <param name="data"></param>
//            public void SetMetadata<T>(T data)
//            {
//                _metadata[typeof(T)] = data;
//            }
//        }


//    }

using LimitlessNonsense.Metadata;
using System.Text.Json.Serialization;

namespace LimitlessNonsense;

[JsonConverter(typeof(ContextMessageJsonConverter))]
public sealed record ContextMessage
{
    /// <summary>
    /// Unique ID for this message
    /// </summary>
    public Guid ID { get; }

    /// <summary>
    /// Role that produced this message
    /// </summary>
    public MessageRole Role { get; }

    /// <summary>
    /// Importance of this message
    /// </summary>
    public Importance Importance { get; set; } = Importance.Normal;

    private readonly Dictionary<Type, object?> _metadata = [];

    public ContextMessage(MessageRole role, Importance importance = Importance.Normal, string? content = null, Guid? guid = null)
    {
        ID = guid ?? Guid.NewGuid();
        Importance = importance;
        Role = role;
        Content = content ?? "";
    }

    #region metadata
    /// <summary>
    /// Set or overwrite the metadata of the given type
    /// </summary>
    /// <typeparam name="TMetadata"></typeparam>
    /// <param name="metadata"></param>
    public void SetMetadata<TMetadata>(TMetadata metadata)
        where TMetadata : class, IMessageMetadata
    {
        _metadata[typeof(TMetadata)] = metadata;
    }

    /// <summary>
    /// Try to get metadata of the given type
    /// </summary>
    /// <typeparam name="TMetadata"></typeparam>
    /// <returns></returns>
    public TMetadata? TryGetMetadata<TMetadata>()
        where TMetadata : class, IMessageMetadata
    {
        return (TMetadata?)_metadata.GetValueOrDefault(typeof(TMetadata), null);
    }

    /// <summary>
    /// Returns true if metadata of the given type is present
    /// </summary>
    /// <typeparam name="TMetadata"></typeparam>
    /// <returns></returns>
    public bool HasMetadata<TMetadata>()
        where TMetadata : class, IMessageMetadata
    {
        return TryGetMetadata<TMetadata>() != null;
    }

    /// <summary>
    /// Returns all metadata entries as type/value pairs (for serialization)
    /// </summary>
    internal IEnumerable<KeyValuePair<Type, object?>> GetMetadataEntries() => _metadata;
    #endregion

    #region content
    /// <summary>
    /// <see cref="Prefix"/> + <see cref="Content"/> + <see cref="Suffix"/> will be sent to the LLM as the message
    /// </summary>
    public string Prefix { get; set; } = "";

    /// <summary>
    /// <see cref="Prefix"/> + <see cref="Content"/> + <see cref="Suffix"/> will be sent to the LLM as the message
    /// </summary>
    public string Content { get; set; } = "";

    /// <summary>
    /// <see cref="Prefix"/> + <see cref="Content"/> + <see cref="Suffix"/> will be sent to the LLM as the message
    /// </summary>
    public string Suffix { get; set; } = "";
    #endregion
}

[Flags]
public enum MessageRole
{
    System = 1,
    Assistant = 2,
    Reasoning = 4,
    User = 8,
    Tool = 16,
    Summary = 32,
}

public enum Importance
{
    VeryHigh = 2,
    High = 1,
    Normal = 0,
    Low = -1,
    VeryLow = -2,

    Ephemeral = -3,
}
//}