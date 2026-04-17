using LimitlessNonsense.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LimitlessNonsense;

[JsonConverter(typeof(MessageJsonConverter))]
public sealed record Message
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
    public MessageImportance Importance { get; set; } = MessageImportance.Normal;

    private readonly Dictionary<Type, object?> _metadata = [];

    public Message(MessageRole role, MessageImportance importance = MessageImportance.Normal, string? content = null, Guid? guid = null)
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
    /// Try to get metadata of the given type
    /// </summary>
    /// <typeparam name="TMetadata"></typeparam>
    /// <returns></returns>
    public bool TryGetMetadata<TMetadata>(out TMetadata? value)
        where TMetadata : class, IMessageMetadata
    {
        if (!_metadata.TryGetValue(typeof(TMetadata), out var obj))
        {
            value = null;
            return false;
        }

        value = (TMetadata)obj!;
        return true;
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
    /// Set or overwrite the metadata by runtime type (for deserialization, avoids reflection at call site)
    /// </summary>
    /// <param name="type"></param>
    /// <param name="metadata"></param>
    internal void SetMetadata(Type type, IMessageMetadata metadata)
    {
        _metadata[type] = metadata;
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
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageRole
{
    System = 1,
    Assistant = 2,
    Reasoning = 4,
    User = 8,
    Tool = 16,
    Summary = 32,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageImportance
    : long
{
    VeryHigh = 2,
    High = 1,
    Normal = 0,
    Low = -1,
    VeryLow = -2,

    Ephemeral = -3,
}

internal sealed class MessageJsonConverter
    : JsonConverter<Message>
{
    public override Message Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Parse JSON
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        // Read basic properties
        var message = new Message(
            (MessageRole)root.GetProperty("role").GetInt32(),
            (MessageImportance)root.GetProperty("importance").GetInt32(),
            root.GetProperty("content").GetString() ?? "",
            root.GetProperty("id").GetGuid()
        )
        {
            Prefix = root.GetProperty("prefix").GetString() ?? "",
            Suffix = root.GetProperty("suffix").GetString() ?? "",
        };

        // Get metadata from JSON
        if (root.TryGetProperty("metadata", out var metadataElement))
        {
            foreach (var item in metadataElement.EnumerateArray())
            {
                // Each metadata item must have $type
                if (!item.TryGetProperty("$type", out var typeProperty))
                    throw new JsonException("Metadata entry is missing '$type' property.");

                // $type must be a string
                var typeName = typeProperty.GetString()
                    ?? throw new JsonException("Metadata entry '$type' property is null.");

                // String must refer to a valid type
                var type = Type.GetType(typeName)
                    ?? throw new JsonException($"Cannot resolve metadata type '{typeName}'.");

                // Type must derive from IMessageMetadata
                if (!typeof(IMessageMetadata).IsAssignableFrom(type))
                    throw new JsonException($"Type '{typeName}' does not implement IMessageMetadata and cannot be deserialized as metadata.");

                // The `value` property contains the serialised type itself, deserialise that
                var value = (IMessageMetadata)(item.GetProperty("value").Deserialize(type, options)
                    ?? throw new JsonException($"Deserialized null value for metadata type '{typeName}'."));

                // Attach the metadata
                message.SetMetadata(type, value);
            }
        }

        return message;
    }

    public override void Write(Utf8JsonWriter writer, Message value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("id", value.ID);
        writer.WriteNumber("role", (int)value.Role);
        writer.WriteNumber("importance", (int)value.Importance);
        writer.WriteString("prefix", value.Prefix);
        writer.WriteString("content", value.Content);
        writer.WriteString("suffix", value.Suffix);

        writer.WriteStartArray("metadata");
        foreach (var (metaType, metaValue) in value.GetMetadataEntries().Where(a => a.Value != null))
        {
            writer.WriteStartObject();
            writer.WriteString("$type", $"{metaType.FullName}, {metaType.Assembly.GetName().Name}");
            writer.WritePropertyName("value");
            JsonSerializer.Serialize(writer, metaValue, metaType, options);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}