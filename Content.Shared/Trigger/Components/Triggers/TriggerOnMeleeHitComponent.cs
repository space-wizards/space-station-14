using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Creates a trigger when this entity melees, i.e. <see cref="MeleeHitEvent"/>.
/// </summary>
/// <remarks>Despite the name this event happens on every melee swing, even if nothing is hit.</remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnMeleeHitComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The mode in which this component chooses how to trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TriggerOnMeleeHitMode Mode = TriggerOnMeleeHitMode.OnceOnHit;

    /// <summary>
    /// If true, the "user" of the trigger is the first entity hit by the melee. "First" is arbitrary.
    /// if false, user is the entity which attacked with the melee weapon.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TargetIsUser;
}

[Serializable, NetSerializable]
public enum TriggerOnMeleeHitMode
{
    /// <summary>
    /// One trigger is created only when nothing is hit.
    /// </summary>
    OnMiss,

    /// <summary>
    /// One trigger is always created when swinging the weapon.
    /// </summary>
    OnSwing,

    /// <summary>
    /// One trigger is created only when hitting a target.
    /// </summary>
    OnceOnHit,

    /// <summary>
    /// A trigger is created only when hitting a target, and for every target.
    /// </summary>
    EveryHit,
}
