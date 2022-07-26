namespace Content.Server.AI.HTN;

/// <summary>
/// AKA Method. This is a branch available for a compound task.
/// </summary>
[DataDefinition]
public sealed class HTNBranch
{
    // Made this its own class if we ever need to change it.
    [ViewVariables, DataField("preconditions")]
    public List<HTNPrecondition> Preconditions = new();

    [ViewVariables, DataField("tasks")]
    public List<HTNTask> Tasks = new();
}
