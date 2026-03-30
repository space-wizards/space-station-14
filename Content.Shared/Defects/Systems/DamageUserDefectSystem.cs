using Content.Shared.Damage.Systems;
using Content.Shared.Defects.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;

namespace Content.Shared.Defects.Systems;

/// <summary>
/// Burns the wielder for a small amount of heat damage on every melee swing.
/// Fires regardless of whether the attack connects - only use on Melee weapons
/// </summary>
public sealed class DamageUserDefectSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageUserDefectComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<DamageUserDefectComponent> ent, ref MeleeHitEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.Damage == null)
            return;

        _damageable.TryChangeDamage(args.User, ent.Comp.Damage, origin: ent.Owner);
    }
}
