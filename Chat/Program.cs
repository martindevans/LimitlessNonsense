using System.Text.Json;
using LimitlessNonsense;
using LimitlessNonsense.Cleanup;
using LimitlessNonsense.Metadata;
using LimitlessNonsense.Middleware;
using LimitlessNonsense.Middleware.Metadata;
using LimitlessNonsense.Middleware.Metadata.Add;
using LimitlessNonsense.Middleware.Time;
using static LimitlessNonsense.Cleanup.Trigger;
using static LimitlessNonsense.Cleanup.Condition;
using static LimitlessNonsense.Cleanup.Actions.ContextAction;

var policies = new CleanupPolicy[]
{
    // Apply summary if it has finished
    new(
        Always(),
        True(),
        Sequence([
            EndSummarise(block:false),
            RemoveIntermediateLinkedToolUpdates()
        ])
    ),
    
    // After every message do some cleanup
    new(
        Always(),
        True(),
        Sequence([
            ImportanceRemoval(MessageImportance.VeryLow, depth: 8),
            RemoveRole(MessageRole.Reasoning, depth: 8),
            RemoveRole(MessageRole.Tool, depth: 8)
        ])
    ),

    // Summarise when idle for a short time to save space
    new(
        Idle(TimeSpan.FromMinutes(7)),
        ContextFillFactor(0.75) & Changed(),
        Sequence([
            BeginSummarise(keepEnd: 8),
            EndSummarise(block:false)
        ])
    ),

    // Summarise when idle for a long while
    new(
        Idle(TimeSpan.FromHours(1)),
        Changed(),
        Sequence([
            BeginSummarise(keepStart: 0, keepEnd: 0),
            EndSummarise(block:false)
        ])
    ),

    // Last ditch effort to free up space
    new(
        Always(),
        ContextFillFactor(0.95),
        Sequence([
            EndSummarise(block:true),
            RemoveRole(MessageRole.Reasoning, depth: 2),
            RemoveRole(MessageRole.Tool, depth: 4),
            ImportanceRemoval(MessageImportance.VeryLow, depth: 2),
            ImportanceRemoval(MessageImportance.Low, depth: 4),
            ImportanceRemoval(MessageImportance.Normal, depth: 6),
            RemoveRole(MessageRole.Reasoning | MessageRole.Tool, depth: 0),
            BeginSummarise(keepEnd:2),
            EndSummarise(block:true),
            ImportanceRemoval(MessageImportance.VeryLow, depth: 0),
            ImportanceRemoval(MessageImportance.Low, depth: 0),
            ImportanceRemoval(MessageImportance.Normal, depth: 0),
            Repeat(
                RemoveOldest(~MessageRole.System)
            ),
        ])
    ),
};

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNameCaseInsensitive = true
};

var json = JsonSerializer.Serialize(policies, options);
Console.WriteLine(json);
var output = JsonSerializer.Deserialize<CleanupPolicy[]>(json, options);

var pipeline = new Pipeline<int>([
    new AddMessageCreationTimeMetadata<int>(),
    new DateChangedMessage<int>(),
    new ElapsedTimeMessage<int>(TimeSpan.FromMinutes(10)),
    new AddMessageCreationTimePrefix<int>(),
    new AddMessageSenderPrefix<int>()
]);
    



var sys = new Message(MessageRole.System, MessageImportance.VeryHigh, "System Prompt", Guid.NewGuid());
sys.SetMetadata(new MessageCreationTime(DateTime.UtcNow - TimeSpan.FromMinutes(30)));

var ctx = new MiddlewareContext<int>(
    [
        sys
    ],
    DateTime.UtcNow,
    new Message(MessageRole.User, MessageImportance.Normal, "Hi"),
    0
);

await pipeline.Apply(ctx);

Console.WriteLine(ctx);