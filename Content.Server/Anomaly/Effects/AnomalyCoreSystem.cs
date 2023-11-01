using Content.Server.Anomaly.Components;
using Content.Server.Cargo.Systems;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// <see cref="AnomalyCoreComponent""/>
/// </summary>
public sealed class AnomalyCoreSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AnomalyCoreComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AnomalyCoreComponent, PriceCalculationEvent>(OnGetPrice);
        SubscribeLocalEvent<AnomalyCoreComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<AnomalyCoreComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }
    private void OnMapInit(Entity<AnomalyCoreComponent> core, ref MapInitEvent args)
    {
        core.Comp.DecayMoment = _gameTiming.CurTime + TimeSpan.FromSeconds(core.Comp.TimeToDecay);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AnomalyCoreComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.IsDecayed)
            {
                //When time runs out, we completely decompose
                if (component.DecayMoment < _gameTiming.CurTime)
                    Decay(uid, component);
            }
            if (component.IsCollapsing)
            {
                if (component.CollapseMoment < _gameTiming.CurTime)
                    Collapse(uid, component);
            }
        }
    }
    private void OnGetPrice(Entity<AnomalyCoreComponent> core, ref PriceCalculationEvent args)
    {
        if (core.Comp.IsDecayed)
        {
            args.Price = core.Comp.EndPrice;
            return;
        }

        var timeLeft = core.Comp.DecayMoment - _gameTiming.CurTime;
        var lerp = (double) (timeLeft.TotalSeconds / core.Comp.TimeToDecay);
        lerp = Math.Clamp(lerp, 0, 1);

        args.Price = MathHelper.Lerp(core.Comp.EndPrice, core.Comp.StartPrice, lerp);
    }

    private void Decay(EntityUid uid, AnomalyCoreComponent component)
    {
        _appearance.SetData(uid, AnomalyCoreVisuals.Decaying, false);
        component.IsDecayed = true;
    }

    #region Collapsing
    private void OnActivateInWorld(Entity<AnomalyCoreComponent> core, ref ActivateInWorldEvent args)
    {
        StartCollapsing(core);
    }

    private void OnUseInHand(Entity<AnomalyCoreComponent> core, ref UseInHandEvent args)
    {
        StartCollapsing(core);
    }

    private void StartCollapsing(Entity<AnomalyCoreComponent> core)
    {
        if (core.Comp.IsDecayed || core.Comp.IsCollapsing)
            return;

        _audio.PlayPvs(core.Comp.ChargingSound, core);
        core.Comp.IsCollapsing = true;
        core.Comp.CollapseMoment = _gameTiming.CurTime + TimeSpan.FromSeconds(core.Comp.TimeToCollapse);
    }
    private void Collapse(EntityUid uid, AnomalyCoreComponent component)
    {
        _audio.PlayPvs(component.CollapseSound, uid);
        var ev = new AnomalyPulseEvent(0.5f, 0.5f);
        RaiseLocalEvent(uid, ref ev);
        QueueDel(uid);
    }
    #endregion
}
