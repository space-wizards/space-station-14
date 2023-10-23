using Content.Server.Popups;
using Content.Server.Tesla.Components;
using Content.Server.Lightning;
using Content.Server.Lightning.Components;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Robust.Shared.Timing;

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
        SubscribeLocalEvent<TeslaGroundingRodComponent, HitByLightningEvent>(OnHitByLightning);
    }

    private void OnInteractHand(Entity<TeslaGroundingRodComponent> grounding, ref InteractHandEvent args)
    {
        Toggle(grounding, !grounding.Comp.Enabled);
    }

    private void OnHitByLightning(Entity<TeslaGroundingRodComponent> grounding, ref HitByLightningEvent args)
    {
        _appearance.SetData(grounding.Owner, TeslaCoilVisuals.Lightning, true);
        grounding.Comp.LightningEndTime = _gameTiming.CurTime + grounding.Comp.LightningTime;
        grounding.Comp.IsSparking = true;
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

    private void Toggle(Entity<TeslaGroundingRodComponent> grounding, bool status)
    {
        if (!TryComp<LightningTargetComponent>(grounding, out var target))
            return;

        target.Priority = status ? grounding.Comp.EnabledPriority : grounding.Comp.DisabledPriority;
        grounding.Comp.Enabled = status;

        _appearance.SetData(grounding, TeslaCoilVisuals.Enabled, status);
        _audio.PlayPvs(status ? grounding.Comp.SoundOpen : grounding.Comp.SoundClose, grounding);
        _popup.PopupEntity(status ? Loc.GetString("tesla-grounding-on") : Loc.GetString("tesla-grounding-off"), grounding);
    }
}
