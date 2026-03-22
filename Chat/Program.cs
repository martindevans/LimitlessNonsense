using LimitlessNonsense;
using LimitlessNonsense.ContextManagement;
using LimitlessNonsense.ContextManagement.Actions;
using System.Text.Json;
using System.Text.Json.Serialization;

var policies = new[]
{
    // Sweep away low priority messages
    new Policy(
        Trigger.Always(),
        Condition.Always(),
        ContextAction.ImportanceRemoval(Importance.VeryLow, depth: 8)
    ),

    // Remove buried reasoning messages
    new Policy(
        Trigger.Idle(TimeSpan.FromMinutes(1)),
        Condition.Always(),
        ContextAction.RemoveRole(MessageRole.Reasoning, depth: 8)
    ),

    // Remove buried tool calls
    new Policy(
        Trigger.Idle(TimeSpan.FromMinutes(1)),
        Condition.Always(),
        ContextAction.RemoveRole(MessageRole.Tool, depth: 8)
    ),

    // Summarise when idle for a short time to save space
    new Policy(
        Trigger.Idle(TimeSpan.FromMinutes(3)),
        Condition.ContextFillFactor(0.75) & Condition.Changed(),
        ContextAction.Summarise(keep: 8)
    ),

    // Summarise when idle for a long while
    new Policy(
        Trigger.Idle(TimeSpan.FromHours(1)),
        Condition.Changed(),
        ContextAction.Summarise(keep: 0)
    ),

    // Last ditch effort to free up space
    new Policy(
        Trigger.Always(),
        Condition.ContextFillFactor(0.9),
        ContextAction.Sequence([
            ContextAction.RemoveRole(MessageRole.Reasoning, depth: 2),
            ContextAction.RemoveRole(MessageRole.Tool, depth: 2),
            ContextAction.ImportanceRemoval(Importance.VeryLow, depth: 2),
            ContextAction.ImportanceRemoval(Importance.Low, depth: 4),
            ContextAction.ImportanceRemoval(Importance.Normal, depth: 6),
            ContextAction.RemoveRole(MessageRole.Reasoning | MessageRole.Tool, depth: 0),
            ContextAction.Summarise(keep: 4),
            ContextAction.ImportanceRemoval(Importance.VeryLow, depth: 0),
            ContextAction.ImportanceRemoval(Importance.Low, depth: 0),
            ContextAction.ImportanceRemoval(Importance.Normal, depth: 0),
            ContextAction.Repeat(
                ContextAction.RemoveOldest(~MessageRole.System)
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