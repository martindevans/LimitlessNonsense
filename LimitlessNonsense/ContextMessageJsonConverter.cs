using System.Text.Json;
using System.Text.Json.Serialization;

namespace LimitlessNonsense;

internal sealed class ContextMessageJsonConverter : JsonConverter<ContextMessage>
{
    public override ContextMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var id = root.GetProperty("id").GetGuid();
        var role = (MessageRole)root.GetProperty("role").GetInt32();
        var importance = (Importance)root.GetProperty("importance").GetInt32();
        var prefix = root.GetProperty("prefix").GetString() ?? "";
        var content = root.GetProperty("content").GetString() ?? "";
        var suffix = root.GetProperty("suffix").GetString() ?? "";

        var message = new ContextMessage(role, importance, content, id)
        {
            Prefix = prefix,
            Suffix = suffix,
        };

        if (root.TryGetProperty("metadata", out var metadataElement))
        {
            foreach (var item in metadataElement.EnumerateArray())
            {
                if (!item.TryGetProperty("$type", out var typeProperty))
                    throw new JsonException("Metadata entry is missing '$type' property.");

                var typeName = typeProperty.GetString()
                    ?? throw new JsonException("Metadata entry '$type' property is null.");

                var type = Type.GetType(typeName)
                    ?? throw new JsonException($"Cannot resolve metadata type '{typeName}'.");

                if (!typeof(IMessageMetadata).IsAssignableFrom(type))
                    throw new JsonException($"Type '{typeName}' does not implement IMessageMetadata and cannot be deserialized as metadata.");

                var value = (IMessageMetadata)(item.GetProperty("value").Deserialize(type, options)
                    ?? throw new JsonException($"Deserialized null value for metadata type '{typeName}'."));

                typeof(ContextMessage)
                    .GetMethod(nameof(ContextMessage.SetMetadata))!
                    .MakeGenericMethod(type)
                    .Invoke(message, [value]);
            }
        }

        return message;
    }

    public override void Write(Utf8JsonWriter writer, ContextMessage value, JsonSerializerOptions options)
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
