using LimitlessNonsense.ContextManagement;
using LimitlessNonsense.ContextManagement.Actions;
using System.Text.Json;
using System.Text.Json.Serialization;
using LimitlessNonsense;
using static LimitlessNonsense.ContextManagement.Trigger;
using static LimitlessNonsense.ContextManagement.Condition;
using static LimitlessNonsense.ContextManagement.Actions.ContextAction;

var policies = new Policy[]
{
    // Sweep away low priority messages
    new(
        Always(),
        True(),
        ImportanceRemoval(Importance.VeryLow, depth: 8)
    ),

    // Remove buried reasoning messages
    new(
        Idle(TimeSpan.FromMinutes(1)),
        True(),
        RemoveRole(MessageRole.Reasoning, depth: 8)
    ),

    // Remove buried tool calls
    new(
        Idle(TimeSpan.FromMinutes(1)),
        True(),
        RemoveRole(MessageRole.Tool, depth: 8)
    ),

    // Summarise when idle for a short time to save space
    new(
        Idle(TimeSpan.FromMinutes(3)),
        ContextFillFactor(0.75) & Changed(),
        Summarise(keep: 8)
    ),

    // Summarise when idle for a long while
    new(
        Idle(TimeSpan.FromHours(1)),
        Changed(),
        Summarise(keep: 0)
    ),

    // Last ditch effort to free up space
    new(
        Always(),
        ContextFillFactor(0.9),
        Sequence([
            RemoveRole(MessageRole.Reasoning, depth: 2),
            RemoveRole(MessageRole.Tool, depth: 2),
            ImportanceRemoval(Importance.VeryLow, depth: 2),
            ImportanceRemoval(Importance.Low, depth: 4),
            ImportanceRemoval(Importance.Normal, depth: 6),
            RemoveRole(MessageRole.Reasoning | MessageRole.Tool, depth: 0),
            Summarise(keep: 4),
            ImportanceRemoval(Importance.VeryLow, depth: 0),
            ImportanceRemoval(Importance.Low, depth: 0),
            ImportanceRemoval(Importance.Normal, depth: 0),
            Repeat(
                RemoveOldest(~MessageRole.System)
            ),
        ])
    ),
};

var json = JsonSerializer.Serialize(policies, new JsonSerializerOptions
{
    WriteIndented = true,
    Converters =
    {
        new JsonStringEnumConverter(),
    }
});
Console.WriteLine(json);
var output = JsonSerializer.Deserialize<ContextAction[]>(json);