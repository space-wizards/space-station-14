using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN;

/// <summary>
/// Repeats the tasks continuously until any of them fails.
/// </summary>
[Prototype("htnRepeating")]
public sealed class HTNRepeatingTask : HTNTask
{
    [DataField("tasks", required: true, customTypeSerializer:typeof(HTNTaskListSerializer))]
    public List<string> Tasks = default!;
}
