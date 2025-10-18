using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective has a target number of something.
/// When the objective is assigned it randomly picks this target from a minimum to a maximum.
/// </summary>
[RegisterComponent, Access(typeof(NumberObjectiveSystem))]
public sealed partial class NumberObjectiveComponent : Component
{
    /// <summary>
    /// Number to use in the objective condition.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Target;

    /// <summary>
    /// Minimum number for target to roll.
    /// </summary>
    [DataField(required: true)]
    public int Min;

    /// <summary>
    /// Maximum number for target to roll.
    /// </summary>
    [DataField(required: true)]
    public int Max;

    /// <summary>
    /// Optional title locale id, passed "count" with <see cref="Target"/>.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? Title;

    /// <summary>
    /// Optional description locale id, passed "count" with <see cref="Target"/>.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? Description;
}
