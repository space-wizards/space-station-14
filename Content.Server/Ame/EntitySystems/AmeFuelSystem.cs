using Content.Server.Ame.Components;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Robust.Shared.Timing;

namespace Content.Server.Ame.EntitySystems;

/// <summary>
/// Adds fuel level info to examine on fuel jars and handles emagged fuel leaking.
/// </summary>
public sealed class AmeFuelSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmeFuelContainerComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<AmeFuelContainerComponent, GotEmaggedEvent>(OnEmagged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AmeFuelContainerComponent, ExplosiveComponent, EmaggedComponent>();
        while (query.MoveNext(out var uid, out var comp, out var explosive, out var _))
        {
            // stops updating this fuel jar once its empty
            // in the future maybe ame could be refueled, then this would require emagging again once it empties out
            if (comp.FuelAmount < 1)
            {
                RemComp<EmaggedComponent>(uid);
                continue;
            }

            var now = _timing.CurTime;
            if (now < comp.NextLeak)
                continue;

            comp.NextLeak = now + comp.LeakDelay;

            // use up fuel
            comp.FuelAmount -= Math.Min(comp.FuelAmount, comp.LeakedFuel);

            // explode but make sure it can explode in the future
            _explosion.TriggerExplosive(uid, explosive);
            explosive.Exploded = false;
        }
    }

    private void OnExamined(EntityUid uid, AmeFuelContainerComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        // less than 25%: amount < capacity / 4 = amount * 4 < capacity
        var low = comp.FuelAmount * 4 < comp.FuelCapacity;
        args.PushMarkup(Loc.GetString("ame-fuel-container-component-on-examine-detailed-message",
            ("colorName", low ? "darkorange" : "orange"),
            ("amount", comp.FuelAmount),
            ("capacity", comp.FuelCapacity)));
    }

    private void OnEmagged(EntityUid uid, AmeFuelContainerComponent comp, ref GotEmaggedEvent args)
    {
        // don't waste a charge if there is no fuel to leak or it cant explode
        if (comp.FuelAmount < 1 || !HasComp<ExplosiveComponent>(uid))
            return;

        args.Handled = true;

        // don't instantly explode, give the emagger a little time to react
        comp.NextLeak = _timing.CurTime + comp.LeakDelay;
    }
}
