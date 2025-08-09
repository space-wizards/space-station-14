using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanStunSystem : EntitySystem
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanStunComponent, HitscanHitEntityEvent>(OnHitscanHit, after: [ typeof(HitscanReflectSystem) ]);
    }

    private void OnHitscanHit(Entity<HitscanStunComponent> hitscan, ref HitscanHitEntityEvent args)
    {
        if (args.Canceled)
            return;

        _stamina.TakeStaminaDamage(args.HitEntity, hitscan.Comp.StaminaDamage, source: args.Shooter);
    }
}
