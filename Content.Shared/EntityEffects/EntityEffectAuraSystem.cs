using Content.Shared.Alert;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;
using Content.Shared.EntityEffects;

namespace Content.Shared.EntityEffects;

public sealed class EntityEffectAuraSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly AlertsSystem _alert = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityEffectAuraComponent, MapInitEvent>(OnPendingMapInit);
    }

    private void OnPendingMapInit(Entity<EntityEffectAuraComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextEntityEffect = _timing.CurTime + ent.Comp.Interval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EntityEffectAuraComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextEntityEffect > _timing.CurTime)
                continue;

            comp.NextEntityEffect = _timing.CurTime + comp.Interval;

            foreach (var ent in _lookup.GetEntitiesInRange(uid, comp.Radius))
            {

                if (_whitelistSystem.IsWhitelistFail(comp.Whitelist, ent) || _whitelistSystem.IsWhitelistPass(comp.Blacklist, ent))
                    continue;

                _effects.ApplyEffects(ent, comp.Effects, user: uid);

                if (comp.Alert != null && HasComp<AlertsComponent>(ent))
                {
                    // Make the alert display time equal to the damage interval, so that the alert updates with each new damage and disappears if we leave the damage aura
                    var cooldown = (_timing.CurTime, _timing.CurTime + comp.Interval);
                    _alert.ShowAlert(ent, comp.Alert.Value, cooldown: cooldown, autoRemove: true, showCooldown: false);
                }
            }
        }
    }
}
