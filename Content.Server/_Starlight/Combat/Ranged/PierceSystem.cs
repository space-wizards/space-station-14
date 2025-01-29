using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared._Starlight.Weapon;
using Content.Shared._Starlight.Combat.Ranged.Pierce;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Inventory;

namespace Content.Server._Starlight.Combat.Ranged;

public sealed partial class PierceSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PierceableComponent, HitScanPierceAttemptEvent>(OnPierceablePierce);
        SubscribeLocalEvent<PierceableComponent, InventoryRelayedEvent<HitScanPierceAttemptEvent>>(OnArmorPierce);
        base.Initialize();
    }

    private void OnArmorPierce(Entity<PierceableComponent> ent, ref InventoryRelayedEvent<HitScanPierceAttemptEvent> args)
    {
        if ((byte)ent.Comp.Level > (byte)args.Args.Level)
            args.Args.Pierced = false;
    }

    private void OnPierceablePierce(Entity<PierceableComponent> ent, ref HitScanPierceAttemptEvent args)
    {
        if ((byte)ent.Comp.Level > (byte)args.Level)
            args.Pierced = false;
    }
}
