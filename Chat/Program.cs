using System.Text.Json;
using System.Text.Json.Serialization;
using LimitlessNonsense;
using LimitlessNonsense.Cleanup;
using LimitlessNonsense.Cleanup.Actions;
using static LimitlessNonsense.Cleanup.Trigger;
using static LimitlessNonsense.Cleanup.Condition;
using static LimitlessNonsense.Cleanup.Actions.ContextAction;

var slot = new SummarySlot();

var policies = new CleanupPolicy[]
{
    // Apply summary if it has finished
    new(
        Always(),
        True(),
        EndSummarise(slot, block:false)
    ),
    
    // After every message do some cleanup
    new(
        Always(),
        True(),
        Sequence([
            ImportanceRemoval(Importance.VeryLow, depth: 8),
            RemoveRole(MessageRole.Reasoning, depth: 8),
            RemoveRole(MessageRole.Tool, depth: 8)
        ])
    ),

    // Summarise when idle for a short time to save space
    new(
        Idle(TimeSpan.FromMinutes(7)),
        ContextFillFactor(0.75) & Changed(),
        Sequence([
            BeginSummarise(slot, keep: 8),
            EndSummarise(slot, block:false)
        ])
    ),

    // Summarise when idle for a long while
    new(
        Idle(TimeSpan.FromHours(1)),
        Changed(),
        Sequence([
            BeginSummarise(slot, keep: 0),
            EndSummarise(slot, block:false)
        ])
    ),

    // Last ditch effort to free up space
    new(
        Always(),
        ContextFillFactor(0.95),
        Sequence([
            EndSummarise(slot, block:true),
            RemoveRole(MessageRole.Reasoning, depth: 2),
            RemoveRole(MessageRole.Tool, depth: 4),
            ImportanceRemoval(Importance.VeryLow, depth: 2),
            ImportanceRemoval(Importance.Low, depth: 4),
            ImportanceRemoval(Importance.Normal, depth: 6),
            RemoveRole(MessageRole.Reasoning | MessageRole.Tool, depth: 0),
            BeginSummarise(slot, keep: 4),
            EndSummarise(slot, block:true),
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