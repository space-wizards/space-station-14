using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.GreyStation.Weapons.Leech;

/// <summary>
/// Applies leech upon melee hitting an alive mob.
/// Healing does not increase if you wideswing multiple mobs.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LeechOnHitSystem))]
public sealed partial class LeechOnHitComponent : Component
{
    /// <summary>
    /// Damage specifier to change you by.
    /// Must be negative or it will instead hurt you.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Leech = new();
}
