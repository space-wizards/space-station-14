namespace Content.Server.Objectives.Components;

/// <summary>
/// This is used as a generic counter for objectives.
/// Requires NumberObjectiveComponent
/// </summary>
[RegisterComponent]
public sealed partial class CounterConditionComponent : Component
{
    [DataField]
    public int Count;
}
