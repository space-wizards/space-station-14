using Content.Shared._Offbrand.Weapons;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands;
using Content.Shared.StatusEffectNew;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class GunAccuracyStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunAccuracyStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<GunAccuracyStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
        SubscribeLocalEvent<GunAccuracyStatusEffectComponent, StatusEffectRelayedEvent<RelayedGunRefreshModifiersEvent>>(OnGunRefreshModifiers);
    }

    private void OnGunRefreshModifiers(Entity<GunAccuracyStatusEffectComponent> ent, ref StatusEffectRelayedEvent<RelayedGunRefreshModifiersEvent> args)
    {
        args.Args = args.Args with { Args = args.Args.Args with { MinAngle = args.Args.Args.MinAngle * ent.Comp.AngleRangeModifier, MaxAngle = args.Args.Args.MaxAngle * ent.Comp.AngleRangeModifier } };
    }

    private void OnStatusEffectApplied(Entity<GunAccuracyStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        foreach (var item in _hands.EnumerateHeld(args.Target))
            if (HasComp<GunComponent>(item))
                _gun.RefreshModifiers(item);
    }

    private void OnStatusEffectRemoved(Entity<GunAccuracyStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        foreach (var item in _hands.EnumerateHeld(args.Target))
            if (HasComp<GunComponent>(item))
                _gun.RefreshModifiers(item);
    }
}
