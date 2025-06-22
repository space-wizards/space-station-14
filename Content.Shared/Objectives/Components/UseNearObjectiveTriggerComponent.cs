using Robust.Shared.GameStates;

namespace Content.Shared.Objectives.Components;

/// <summary>
/// Allows an object to affect targets of the UseNearEntityCondition objective.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UseNearObjectiveTriggerComponent : Component
{
    /// <summary>
    /// The range around the object to check for targets.
    /// </summary>
    [DataField]
    public float Range = 5;
}
