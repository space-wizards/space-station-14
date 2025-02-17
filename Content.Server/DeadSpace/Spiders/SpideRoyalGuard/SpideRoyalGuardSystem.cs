// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.DeadSpace.Spiders.SpideRoyalGuard.Components;
using Robust.Shared.Timing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Damage;
using Content.Shared.Alert;
using Content.Shared.Mobs.Systems;

namespace Content.Server.DeadSpace.Spiders.SpideRoyalGuard;

public sealed class SpideRoyalGuardSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpideRoyalGuardComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<SpideRoyalGuardComponent, DamageChangedEvent>(OnDamage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var spideRoyalGuardQuery = EntityQueryEnumerator<SpideRoyalGuardComponent>();
        while (spideRoyalGuardQuery.MoveNext(out var ent, out var spideRoyalGuard))
        {
            if (_gameTiming.CurTime > spideRoyalGuard.TileLeftCheckKing)
                SetGuardian(ent, spideRoyalGuard);
        }
    }

    private void SetGuardian(EntityUid uid, SpideRoyalGuardComponent component)
    {
        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        var entities = _lookup.GetEntitiesInRange<SpiderKingComponent>(_transform.GetMapCoordinates(uid, xform), component.Range);
        bool isPresentAliveKing = false;

        if (entities.Count <= 0)
        {
            component.TileLeftCheckKing = _gameTiming.CurTime + TimeSpan.FromSeconds(1);
            return;
        }

        foreach (var ent in entities)
        {
            if (!_mobState.IsDead(ent))
            {
                isPresentAliveKing = true;
                break;
            }
        }

        if (!isPresentAliveKing)
        {
            component.IsGuards = false;
            _alertsSystem.ClearAlert(uid, component.SpiderGuardAlert);
        }
        else
        {
            component.IsGuards = true;
            _alertsSystem.ShowAlert(uid, component.SpiderGuardAlert);
        }

        component.TileLeftCheckKing = _gameTiming.CurTime + TimeSpan.FromSeconds(1);
    }

    private void OnMeleeHit(EntityUid uid, SpideRoyalGuardComponent component, MeleeHitEvent args)
    {
        if (component.IsGuards)
            args.BonusDamage = args.BaseDamage * 2;
    }

    private void OnDamage(EntityUid uid, SpideRoyalGuardComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;

        if (component.IsGuards)
        {

            var damage = args.DamageDelta * 0.5;
            _damageable.TryChangeDamage(uid, -damage);
        }
    }

}
