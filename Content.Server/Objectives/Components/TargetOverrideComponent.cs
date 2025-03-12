namespace Content.Server.Objectives.Components;

/// <summary>
/// Overrides a target objective when receiving if it has <see cref="TargetObjectiveOverrideComponent"/>.
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
