using Content.Shared.Alert;
using Content.Shared.Body.Systems;
using Content.Shared.Changeling.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.Timing;

namespace Content.Shared.Changeling;

public sealed class ChangelingBiomassSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingBiomassComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingBiomassComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChangelingBiomassComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnMapInit(EntityUid uid, ChangelingBiomassComponent component, MapInitEvent args)
    {
        _alerts.ShowAlert(uid, component.BiomassAlert);

        component.NextUpdate = _timing.CurTime + component.UpdateInterval;
    }

    private void OnShutdown(EntityUid uid, ChangelingBiomassComponent component, ComponentShutdown args)
    {
        _alerts.ClearAlert(uid, component.BiomassAlert);
    }

    private void OnRejuvenate(EntityUid uid, ChangelingBiomassComponent component, RejuvenateEvent args)
    {
        component.CurrentBiomass = component.MaxBiomass;
        Dirty(uid, component);
    }

    public void ModifyBiomass(Entity<ChangelingBiomassComponent?> ent, float value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.CurrentBiomass = Math.Clamp(ent.Comp.CurrentBiomass + value, ent.Comp.MinBiomass, ent.Comp.SoftCapMaximum ? Int32.MaxValue : ent.Comp.MaxBiomass);

        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChangelingBiomassComponent>();
        while (query.MoveNext(out var uid, out var biomass))
        {
            if (_timing.CurTime < biomass.NextUpdate)
                continue;

            biomass.NextUpdate = _timing.CurTime + biomass.UpdateInterval;
            ModifyBiomass(uid, -biomass.BiomassDecay);

            if (biomass.CurrentBiomass <= biomass.MinBiomass && biomass.GibOnEmpty)
                _body.GibBody(uid, true); // TODO: When we have true form etc, you should only gib when you CRIT at 0 biomass. Without symptoms however this is a better option so the ling has something to worry about.
        }
    }
}
