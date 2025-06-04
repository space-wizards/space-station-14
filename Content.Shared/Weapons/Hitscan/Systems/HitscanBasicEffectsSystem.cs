using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Player;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanBasicEffectsSystem : EntitySystem
{
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicEffectsComponent, HitscanHitEntityEvent>(OnHitscanHit, after: [ typeof(HitscanBasicDamageSystem) ]);
    }

    private void OnHitscanHit(Entity<HitscanBasicEffectsComponent> hitscan, ref HitscanHitEntityEvent args)
    {
        if (args.Canceled|| Deleted(args.HitEntity))
            return;

        TryComp<HitscanBasicDamageComponent>(hitscan, out var hitscanDamageComp);

        var dmg = hitscanDamageComp?.DamageDealt ?? new DamageSpecifier();

        if (dmg.AnyPositive() && hitscan.Comp.HitColor != null)
            _color.RaiseEffect(hitscan.Comp.HitColor.Value, new List<EntityUid>() { args.HitEntity }, Filter.Pvs(args.HitEntity, entityManager: EntityManager));

        // TODO get fallback position for playing hit sound.
        _gun.PlayImpactSound(args.HitEntity, dmg, hitscan.Comp.Sound, hitscan.Comp.ForceSound);
    }
}
