using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Melee;
using Content.Shared.Throwing;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Melee;

public class DeflectThrownObjectsSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly EntityLookupSystem _entLookup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;

    private EntityQuery<MeleeWeaponComponent> _melee;

    public override void Initialize()
    {
        base.Initialize();
        _melee = GetEntityQuery<MeleeWeaponComponent>();

        SubscribeLocalEvent<DeflectThrownObjectsComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid ent, DeflectThrownObjectsComponent comp, MeleeHitEvent args)
    {
        if(!_gameTiming.IsFirstTimePredicted)
            return;

        // Check that it was a wide attack
        if(args.Direction == null)
            return;

        if(!_melee.TryComp(ent, out var melee))
            return;

        var trns = Transform(args.User);
        var deflected = _entLookup.GetEntitiesInArc(trns.Coordinates, melee.Range, new Angle(args.Direction.Value), (float) melee.Angle.Degrees, LookupFlags.Dynamic);
        foreach(var obj in deflected) {
            if(!_entManager.HasComponent<ThrownItemComponent>(obj))
                continue;

            _throwing.TryThrow(
                obj,
                args.Direction.Value,
                user: args.User,
                baseThrowSpeed: comp.DeflectSpeed
            );
            _audioSystem.PlayPvs(comp.DeflectSound, ent);
            _damage.TryChangeDamage(obj, melee.Damage);
        }
    }
}
