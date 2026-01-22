using Content.Shared.Alert;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed class DamageAuraSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly AlertsSystem _alert = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageAuraComponent, MapInitEvent>(OnPendingMapInit);
    }

    private void OnPendingMapInit(EntityUid uid, DamageAuraComponent component, MapInitEvent args)
    {
        component.NextDamage = _timing.CurTime + TimeSpan.FromSeconds(1f);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<DamageAuraComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextDamage > curTime)
                continue;

            comp.NextDamage = curTime + TimeSpan.FromSeconds(comp.Interval);

            foreach (var ent in _lookup.GetEntitiesInRange(uid, comp.Radius))
            {
                if (!HasComp<DamageableComponent>(ent))
                    continue;

                if (_whitelistSystem.IsWhitelistFail(comp.Whitelist, ent) || _whitelistSystem.IsWhitelistPass(comp.Blacklist, ent))
                    continue;

                _damageable.ChangeDamage(ent, comp.Damage, true, false);

                if (comp.Alert != null && HasComp<AlertsComponent>(ent))
                {
                    // Make the alert display time equal to the damage interval, so that the alert updates with each new damage and disappears if we leave the damage aura
                    var cooldown = (_timing.CurTime, _timing.CurTime + TimeSpan.FromSeconds(comp.Interval));
                    _alert.ShowAlert(ent, comp.Alert.Value, cooldown: cooldown, autoRemove: true, showCooldown: false);
                }
            }
        }
    }
}
