using System.Threading;
using Content.Server.Damage.Systems;
using Content.Shared.Damage;

namespace Content.Server.Damage.Components;

[RegisterComponent]
[AutoGenerateComponentState]
[Access(typeof(DamageOnDraggingCritSystem))]
public sealed partial class DamageOnDraggingCritComponent : Component
{
    /// <summary>
    /// Whether or not to check for dragging damage on this entity
    /// </summary>
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;

    /// <summary>
    /// The amount of time between drag damage checks.
    /// </summary>
    [DataField("interval"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Interval = TimeSpan.FromSeconds(0.25);

    /// <summary>
    /// The last time interval we checked for drag damage. Used to
    /// scale Damage with the time between LastInterval and CurTime.
    /// </summary>
    public TimeSpan LastInterval = default!;

    public CancellationTokenSource? TimerCancel;

    /// <summary>
    /// The distance this mob has been dragged since the last
    /// damage check
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float DistanceDragged = 0.0f;

    /// <summary>
    /// The amount of damage to add per second of being dragged.
    /// This is scaled by `CurTime - LastInterval`.
    /// </summary>
    [DataField("damage", required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// The units of distance a mob has to be dragged per second to
    /// cause damage. Ideally, walking slow or right-click dragging
    /// should not cause drag damage.
    /// This is scaled by Interval.
    /// </summary>
    [DataField("distanceThreshold"), ViewVariables(VVAccess.ReadWrite)]
    public float DistanceThreshold = 2.2f;
}
