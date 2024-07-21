using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(DamageOnDraggingCritSystem))]
public sealed partial class DamageOnDraggingCritComponent : Component
{
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// The amount of time between drag damage checks.
    /// </summary>
    [DataField("interval"), ViewVariables(VVAccess.ReadWrite)]
    public float Interval = 0.25f;

    /// <summary>
    /// The amount of time elapsed since the last damage check
    /// </summary>
    [DataField("intervalTimer"), ViewVariables(VVAccess.ReadOnly)]
    public float IntervalTimer = 0.0f;

    /// <summary>
    /// The distance this mob has been dragged since the last
    /// damage check
    /// </summary>
    [DataField("distanceDragged"), ViewVariables(VVAccess.ReadOnly)]
    public float DistanceDragged = 0.0f;

    /// <summary>
    /// The amount of damage to add per second of being dragged.
    /// This is scaled by IntervalTimer.
    /// </summary>
    [DataField("damage", required: true), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// The units of distance a mob has to be dragged per second to
    /// cause damage. Ideally, walking slow or right-click dragging
    /// should not cause drag damage.
    /// This is scaled by Interval.
    /// </summary>
    [DataField("distanceThreshold"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float DistanceThreshold = 2.2f;
}
