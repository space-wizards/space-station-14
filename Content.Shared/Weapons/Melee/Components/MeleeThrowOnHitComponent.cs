using Content.Shared.Throwing;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// This is used for a melee weapon that throws whatever gets hit by it in a line
/// until it hits a wall or a time limit is exhausted.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(MeleeThrowOnHitSystem))]
public sealed partial class MeleeThrowOnHitComponent : Component
{
    /// <summary>
    /// The speed at which hit entities should be thrown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Speed = 10f;

    /// <summary>
    /// The maximum distance the hit entity should be thrown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Distance = 20f;

    /// <summary>
    /// Whether or not anchorable entities should be unanchored when hit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ThrowingUnanchorStrength UnanchorOnHit = ThrowingUnanchorStrength.None;

    /// <summary>
    /// How long should this stun the target, if applicable?
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? StunTime;

    /// <summary>
    /// Should this also work on a throw-hit?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ActivateOnThrown;

    /// <summary>
    /// Whether the entity can apply knockback this instance of being thrown.
    /// If true, the entity cannot apply knockback.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool ThrowOnCooldown;

    /// <summary>
    /// Whether this item has hit anyone while it was thrown.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool HitWhileThrown;
}

/// <summary>
/// Raised a weapon entity with <see cref="MeleeThrowOnHitComponent"/> to see if a throw is allowed.
/// </summary>
[ByRefEvent]
public record struct AttemptMeleeThrowOnHitEvent(EntityUid Target, EntityUid? User, bool Cancelled = false, bool Handled = false);

/// <summary>
/// Raised a target entity before it is thrown by <see cref="MeleeThrowOnHitComponent"/>.
/// </summary>
[ByRefEvent]
public record struct MeleeThrowOnHitStartEvent(EntityUid Weapon, EntityUid? User);
