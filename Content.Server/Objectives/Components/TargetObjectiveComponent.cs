using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(TargetObjectiveSystem))]
public sealed partial class TargetObjectiveComponent : Component
{
    /// <summary>
    /// Optional locale id for the objective title.
    /// It is passed "targetName" and "job" arguments.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public LocId? Title;

    /// <summary>
    /// Optional locale id for the objective description.
    /// It is passed "targetName" and "job" arguments.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public LocId? Description;

    /// <summary>
    /// Mind entity id of the target.
    /// This must be set by another system using <see cref="TargetObjectiveSystem.SetTarget"/>.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;
}
