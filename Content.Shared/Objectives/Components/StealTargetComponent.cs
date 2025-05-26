using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Objectives.Components;

/// <summary>
/// Allows an object to become the target of a steal objective
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StealTargetComponent : Component
{
    /// <summary>
    /// The theft group to which this item belongs.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<StealTargetGroupPrototype> StealGroup;
}
