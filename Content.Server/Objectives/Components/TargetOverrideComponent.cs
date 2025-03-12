namespace Content.Server.Objectives.Components;

/// <summary>
/// Sets a target objective to a specific target when receiving it.
/// The objective entity needs to have <see cref="PickSpecificPersonComponent"/>.
/// This component needs to be added to entity receiving the objective.
/// </summary>
[RegisterComponent]
public sealed partial class TargetOverrideComponent : Component
{
    /// <summary>
    /// The entity that should be targeted.
    /// </summary>
    [DataField]
    public EntityUid? Target;
}
