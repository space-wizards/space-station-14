using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Stethoscope.Components;

/// <summary>
///     Adds a verb and action that allows the user to listen to the entity's breathing.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StethoscopeComponent : Component
{
    /// <summary>
    ///     Time between each use of the stethoscope.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1.75);

    /// <summary>
    ///     Last damage that was measured. Used to indicate if breathing is improving or getting worse.
    /// </summary>
    [DataField]
    public FixedPoint2? LastMeasuredDamage;

    [DataField]
    public EntProtoId Action = "ActionStethoscope";

    [DataField]
    public EntityUid? ActionEntity;
}

