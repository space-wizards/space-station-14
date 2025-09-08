using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Shared._Starlight.Weapons.Melee.Events;

/// <summary>
///     Raised directed on the melee weapon entity used to attack something in combat mode,
///     whether through a click attack or wide attack.
/// </summary>
public sealed class AfterMeleeHitEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     The amount of damage dealed by the melee hit.
    /// </summary>
    public readonly DamageSpecifier DealedDamage;

    /// <summary>
    ///     A list containing every hit entity. Can be zero.
    /// </summary>
    public IReadOnlyList<EntityUid> HitEntities;

    /// <summary>
    /// The user who attacked with the melee weapon.
    /// </summary>
    public readonly EntityUid User;

    /// <summary>
    /// The melee weapon used.
    /// </summary>
    public readonly EntityUid Weapon;

    /// <summary>
    /// The direction of the attack.
    /// If null, it was a click-attack.
    /// </summary>
    public readonly Vector2? Direction;

    /// <summary>
    /// Check if this is true before attempting to do something during a melee attack other than changing/adding bonus damage. <br/>
    /// For example, do not spend charges unless <see cref="IsHit"/> equals true.
    /// </summary>
    /// <remarks>
    /// Examining melee weapons calls this event, but with <see cref="IsHit"/> set to false.
    /// </remarks>
    public bool IsHit = true;

    public AfterMeleeHitEvent(List<EntityUid> hitEntities, EntityUid user, EntityUid weapon, DamageSpecifier dealedDamage, Vector2? direction)
    {
        HitEntities = hitEntities;
        User = user;
        Weapon = weapon;
        DealedDamage = dealedDamage;
        Direction = direction;
    }
}