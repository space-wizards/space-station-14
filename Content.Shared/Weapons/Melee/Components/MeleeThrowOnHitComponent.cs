using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// This is used for a melee weapon that throws whatever gets hit by it in a line
/// until it hits a wall or a time limit is exhausted.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(MeleeThrowOnHitSystem))]
[AutoGenerateComponentState]
public sealed partial class MeleeThrowOnHitComponent : Component
{
    /// <summary>
    /// The speed at which hit entities should be thrown.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float Speed = 10f;

    /// <summary>
    /// How long hit entities remain thrown, max.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float Lifetime = 3f;

    /// <summary>
    /// How long we wait to start accepting collision.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinLifetime = 0.05f;

    /// <summary>
    /// Whether or not anchorable entities should be unanchored when hit.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool UnanchorOnHit;

    /// <summary>
    /// Whether or not the throwing behavior occurs by default.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Enabled = true;
}

/// <summary>
/// Component used to track entities that have been yeeted by <see cref="MeleeThrowOnHitComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(MeleeThrowOnHitSystem))]
public sealed partial class MeleeThrownComponent : Component
{
    /// <summary>
    /// The velocity of the throw
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public Vector2 Velocity;

    /// <summary>
    /// How long the throw will last.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float Lifetime;

    /// <summary>
    /// How long we wait to start accepting collision.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinLifetime;

    /// <summary>
    /// At what point in time will the throw be complete?
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    public TimeSpan ThrownEndTime;

    /// <summary>
    /// At what point in time will the <see cref="MinLifetime"/> be exhausted
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    public TimeSpan MinLifetimeTime;

    /// <summary>
    /// the status to which the entity will return when the thrown ends
    /// </summary>
    [DataField]
    public BodyStatus PreviousStatus;
}

/// <summary>
/// Event raised before an entity is thrown by <see cref="MeleeThrowOnHitComponent"/> to see if a throw is allowed.
/// If not handled, the enabled field on the component will be used instead.
/// </summary>
[ByRefEvent]
public record struct AttemptMeleeThrowOnHitEvent(EntityUid Hit, bool Cancelled = false, bool Handled = false);

[ByRefEvent]
public record struct MeleeThrowOnHitStartEvent(EntityUid User, EntityUid Used);

[ByRefEvent]
public record struct MeleeThrowOnHitEndEvent();
