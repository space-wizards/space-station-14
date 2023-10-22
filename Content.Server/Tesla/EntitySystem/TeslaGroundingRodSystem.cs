using Content.Shared.Interaction;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Robust.Shared.Timing;
using Content.Server.Tesla.Components;
using Content.Server.Lightning;
using Content.Server.Power.EntitySystems;
using Content.Shared.Power;
using Content.Server.Lightning.Components;

namespace Content.Server.Tesla.EntitySystems;

public sealed class TeslaGroundingRodSystem : EntitySystem
{

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeslaGroundingRodComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<TeslaGroundingRodComponent, HittedByLightningEvent>(OnHittedLightning);
    }

    private void OnPowerChanged(EntityUid uid, TeslaGroundingRodComponent component, ref PowerChangedEvent args)
    {
        if (!TryComp<LightningTargetComponent>(uid, out var target))
            return;

        target.Priority = args.Powered ? component.EnabledPriority : component.DisabledPriority;
    }

    private void OnHittedLightning(EntityUid uid, TeslaGroundingRodComponent component, ref HittedByLightningEvent args)
    {
        _appearance.SetData(uid, TeslaCoilVisuals.Lightning, true);
        component.LightningEndTime = _gameTiming.CurTime + component.LightningTime;
        component.IsSparking = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TeslaGroundingRodComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.IsSparking)
                continue;

            if (component.LightningEndTime < _gameTiming.CurTime)
            {
                _appearance.SetData(uid, TeslaCoilVisuals.Lightning, false);
                component.IsSparking = false;
            }
        }
    }
}
