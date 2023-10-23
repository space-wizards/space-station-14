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
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeslaGroundingRodComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<TeslaGroundingRodComponent, HitByLightningEvent>(OnHittedLightning);
    }

    private void OnInteractHand(EntityUid uid, TeslaGroundingRodComponent component, InteractHandEvent args)
    {
        Toggle(uid, component, !component.Enabled);
    }

    private void OnHittedLightning(EntityUid uid, TeslaGroundingRodComponent component, ref HitByLightningEvent args)
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

    private void Toggle(EntityUid uid, TeslaGroundingRodComponent component, bool status)
    {
        if (!TryComp<LightningTargetComponent>(uid, out var target))
            return;

        target.Priority = status ? component.EnabledPriority : component.DisabledPriority;
        component.Enabled = status;
        _appearance.SetData(uid, TeslaCoilVisuals.Enabled, status);
        _audio.PlayPvs(status ? component.SoundOpen : component.SoundClose, uid);
        _popup.PopupEntity(status ? Loc.GetString("tesla-grounding-on") : Loc.GetString("tesla-grounding-off"), uid);
    }
}
