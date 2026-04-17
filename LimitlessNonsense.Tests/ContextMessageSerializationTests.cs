using System.Text.Json;
using LimitlessNonsense.Metadata;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class ContextMessageSerializationTests
{
    private static readonly JsonSerializerOptions Options = new();

    [TestMethod]
    public void RoundTrip_NoMetadata()
    {
        var id = Guid.NewGuid();
        var original = new Message(MessageRole.User, MessageImportance.High, "Hello world", id)
        {
            Prefix = "PRE",
            Suffix = "SUF",
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<Message>(json, Options);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(original.ID, deserialized.ID);
        Assert.AreEqual(original.Role, deserialized.Role);
        Assert.AreEqual(original.Importance, deserialized.Importance);
        Assert.AreEqual(original.Prefix, deserialized.Prefix);
        Assert.AreEqual(original.Content, deserialized.Content);
        Assert.AreEqual(original.Suffix, deserialized.Suffix);
    }

    [TestMethod]
    public void RoundTrip_FlagsRole()
    {
        var original = new Message(MessageRole.System | MessageRole.Summary);

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<Message>(json, Options);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(original.Role, deserialized.Role);
    }

    [TestMethod]
    public void RoundTrip_SingleMetadata()
    {
        var original = new Message(MessageRole.User, MessageImportance.Normal, "Hello", Guid.NewGuid());
        original.SetMetadata(new MessageSender("Alice"));

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<Message>(json, Options);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(original.ID, deserialized.ID);

        var sender = deserialized.TryGetMetadata<MessageSender>();
        Assert.IsNotNull(sender);
        Assert.AreEqual("Alice", sender.Name);
    }

    [TestMethod]
    public void RoundTrip_MultipleMetadata()
    {
        var original = new Message(MessageRole.User, MessageImportance.Normal, "Hello", Guid.NewGuid());
        original.SetMetadata(new MessageSender("Bob"));
        original.SetMetadata(new MessageCreationTime(new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc)));

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<Message>(json, Options);

        Assert.IsNotNull(deserialized);

        var sender = deserialized.TryGetMetadata<MessageSender>();
        Assert.IsNotNull(sender);
        Assert.AreEqual("Bob", sender.Name);

        var creationTime = deserialized.TryGetMetadata<MessageCreationTime>();
        Assert.IsNotNull(creationTime);
        Assert.AreEqual(new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc), creationTime.Time);
    }

    [TestMethod]
    public void RoundTrip_AllImportanceLevels()
    {
        foreach (var importance in Enum.GetValues<MessageImportance>())
        {
            var original = new Message(MessageRole.User, importance);
            var json = JsonSerializer.Serialize(original, Options);
            var deserialized = JsonSerializer.Deserialize<Message>(json, Options);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(importance, deserialized.Importance);
        }
    }

    [TestMethod]
    public void Deserialize_TypeNotImplementingIMessageMetadata_ThrowsJsonException()
    {
        var maliciousJson = """
            {
              "id": "00000000-0000-0000-0000-000000000001",
              "role": 8,
              "importance": 0,
              "prefix": "",
              "content": "",
              "suffix": "",
              "metadata": [
                {
                  "$type": "System.String, System.Private.CoreLib",
                  "value": "evil"
                }
              ]
            }
            """;

        Assert.ThrowsExactly<JsonException>(() =>
            JsonSerializer.Deserialize<Message>(maliciousJson, Options));
    }

    [TestMethod]
    public void Deserialize_UnknownType_ThrowsJsonException()
    {
        var json = """
            {
              "id": "00000000-0000-0000-0000-000000000001",
              "role": 8,
              "importance": 0,
              "prefix": "",
              "content": "",
              "suffix": "",
              "metadata": [
                {
                  "$type": "NonExistent.Type, SomeAssembly",
                  "value": {}
                }
              ]
            }
            """;

        Assert.ThrowsExactly<JsonException>(() =>
            JsonSerializer.Deserialize<Message>(json, Options));
    }

    [TestMethod]
    public void Deserialize_MissingTypeProperty_ThrowsJsonException()
    {
        var json = """
            {
              "id": "00000000-0000-0000-0000-000000000001",
              "role": 8,
              "importance": 0,
              "prefix": "",
              "content": "",
              "suffix": "",
              "metadata": [
                {
                  "value": {}
                }
              ]
            }
            """;

        Assert.ThrowsExactly<JsonException>(() =>
            JsonSerializer.Deserialize<Message>(json, Options));
    }

    [TestMethod]
    public void Serialize_ContainsTypeTagInMetadata()
    {
        var original = new Message(MessageRole.Assistant);
        original.SetMetadata(new MessageSender("Charlie"));

        var json = JsonSerializer.Serialize(original, Options);

        Assert.Contains("$type", json);
        Assert.Contains("MessageSender", json);
    }
}
