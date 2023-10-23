using Content.Shared.Interaction;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Power;
using Content.Server.Power.EntitySystems;
using Content.Server.Tesla.Components;
using Content.Server.Lightning;
using Robust.Shared.Timing;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// Generates electricity from lightning bolts
/// </summary>
public sealed class TeslaCoilSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeslaCoilComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<TeslaCoilComponent, HitByLightningEvent>(OnHittedLightning);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TeslaCoilComponent>();
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

    //When interacting, turn the coil on or off.
    private void OnInteractHand(EntityUid uid, TeslaCoilComponent component, InteractHandEvent args)
    {
        ToggleCoil(uid, component, !component.Enabled);
    }

    //When struck by lightning, charge the internal battery
    private void OnHittedLightning(EntityUid uid, TeslaCoilComponent component, ref HitByLightningEvent args)
    {
        if (!component.Enabled)
            return;
        if (!TryComp<BatteryComponent>(uid, out var batteryComponent))
            return;

        _battery.SetCharge(uid, batteryComponent.CurrentCharge + component.ChargeFromLightning);

        _appearance.SetData(uid, TeslaCoilVisuals.Lightning, true);
        component.LightningEndTime = _gameTiming.CurTime + component.LightningTime;
        component.IsSparking = true;
    }

    private void ToggleCoil(EntityUid uid, TeslaCoilComponent component, bool status)
    {
        component.Enabled = status;
        _appearance.SetData(uid, TeslaCoilVisuals.Enabled, status);
        _audio.PlayPvs(status ? component.SoundOpen : component.SoundClose, uid);
        _popup.PopupEntity(status ? Loc.GetString("tesla-coil-on") : Loc.GetString("tesla-coil-off"), uid);
    }
}
