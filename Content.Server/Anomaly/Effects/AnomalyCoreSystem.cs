using Content.Server.Anomaly.Components;
using Content.Server.Cargo.Components;
using Content.Shared.Anomaly;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This component exists for a limited time, and after it expires it modifies the entity, greatly reducing its value and changing its visuals
/// </summary>
public sealed class AnomalyCoreSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AnomalyCoreComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<AnomalyCoreComponent> core, ref MapInitEvent args)
    {
        core.Comp.DecayMoment = _gameTiming.CurTime + TimeSpan.FromSeconds(core.Comp.TimeToDecay);

        if (TryComp<StaticPriceComponent>(core, out var price))
        {
            core.Comp.OldPrice = price.Price;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AnomalyCoreComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.IsDecayed)
                continue;

            //checks every few seconds how much time is left to decompose, and decreases the value of the object
            component.AccumulatedFrametime += frameTime;
            if (component.AccumulatedFrametime < component.UpdateInterval)
                continue;

            var timeLeft = component.DecayMoment - _gameTiming.CurTime;
            var lerp = (double)(timeLeft.TotalSeconds / component.TimeToDecay);
            lerp = Math.Clamp(lerp, 0, 1);

            if (TryComp<StaticPriceComponent>(uid, out var price))
                price.Price = MathHelper.Lerp(component.FuturePrice, component.OldPrice, lerp);

            //When time runs out, we completely decompose
            if (component.DecayMoment < _gameTiming.CurTime)
                Decay(uid, component);

            component.AccumulatedFrametime = 0;
        }
    }

    private void Decay(EntityUid uid, AnomalyCoreComponent component)
    {
        _appearance.SetData(uid, AnomalyCoreVisuals.Decaying, false);
        component.IsDecayed = true;

    }
}
