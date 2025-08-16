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

        SubscribeLocalEvent<HitscanBasicEffectsComponent, HitscanRaycastFiredEvent>(OnHitscanHit, after: [ typeof(HitscanBasicDamageSystem) ]);
    }

    private void OnHitscanHit(Entity<HitscanBasicEffectsComponent> hitscan, ref HitscanRaycastFiredEvent args)
    {
        if (args.Canceled || args.HitEntity == null || Deleted(args.HitEntity))
            return;

        if (TryComp<HitscanBasicDamageComponent>(hitscan, out var hitscanDamageComp)
            && hitscan.Comp.HitColor != null
            && hitscanDamageComp.Damage.AnyPositive())
        {
            _color.RaiseEffect(hitscan.Comp.HitColor.Value,
                new List<EntityUid> { args.HitEntity.Value },
                Filter.Pvs(args.HitEntity.Value, entityManager: EntityManager));
        }

        _gun.PlayImpactSound(args.HitEntity.Value, hitscanDamageComp?.Damage, hitscan.Comp.Sound, hitscan.Comp.ForceSound);
    }
}
