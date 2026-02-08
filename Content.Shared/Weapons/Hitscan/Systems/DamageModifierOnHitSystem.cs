using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class DamageModifierOnHitSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageModifierOnHitComponent, HitscanRaycastFiredEvent>(OnHitscanHit, before: [ typeof(HitscanBasicDamageSystem) ]);
        SubscribeLocalEvent<DamageModifierOnHitComponent, BeforeHitscanDamageDealtEvent>(OnBeforeHitscanDamageDealtEvent);
        SubscribeLocalEvent<DamageModifierOnHitComponent, AttemptHitscanRaycastFiredEvent>(OnAttemptHitscanRaycastFiredEvent);
    }

    private void OnHitscanHit(Entity<DamageModifierOnHitComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        var evnt = new HitscanDamageModifierEvent
        {
            HitscanUid = ent.Owner,
            Modifier = 0.0f,
        };

        RaiseLocalEvent(args.Data.HitEntity.Value, ref evnt);

        ent.Comp.DamageScaler += evnt.Modifier;

        if (ent.Comp.DamageScaler <= 0 || MathHelper.CloseTo(ent.Comp.DamageScaler, 0))
            ent.Comp.DamageScaler = 0;
    }

    private void OnBeforeHitscanDamageDealtEvent(Entity<DamageModifierOnHitComponent> ent, ref BeforeHitscanDamageDealtEvent args)
    {
        args.DamageToDeal *= ent.Comp.DamageScaler;
    }

    private void OnAttemptHitscanRaycastFiredEvent(Entity<DamageModifierOnHitComponent> ent, ref AttemptHitscanRaycastFiredEvent args)
    {
        if (ent.Comp.DamageScaler <= 0)
            args.Cancelled = true;
    }
}
