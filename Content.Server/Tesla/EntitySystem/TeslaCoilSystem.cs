using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Tesla.Components;
using Content.Server.Lightning;
using Content.Shared.Power;
using Content.Shared.Interaction;
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
        SubscribeLocalEvent<TeslaCoilComponent, HitByLightningEvent>(OnHitByLightning);
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
    private void OnInteractHand(Entity<TeslaCoilComponent> tesla, ref InteractHandEvent args)
    {
        ToggleCoil(tesla, !tesla.Comp.Enabled);
    }

    //When struck by lightning, charge the internal battery
    private void OnHitByLightning(Entity<TeslaCoilComponent> tesla, ref HitByLightningEvent args)
    {
        if (!tesla.Comp.Enabled)
            return;
        if (!TryComp<BatteryComponent>(tesla, out var batteryComponent))
            return;

        _battery.SetCharge(tesla, batteryComponent.CurrentCharge + tesla.Comp.ChargeFromLightning);

        _appearance.SetData(tesla, TeslaCoilVisuals.Lightning, true);
        tesla.Comp.LightningEndTime = _gameTiming.CurTime + tesla.Comp.LightningTime;
        tesla.Comp.IsSparking = true;
    }

    private void ToggleCoil(Entity<TeslaCoilComponent> tesla, bool status)
    {
        tesla.Comp.Enabled = status;
        _appearance.SetData(tesla, TeslaCoilVisuals.Enabled, status);
        _audio.PlayPvs(status ? tesla.Comp.SoundOpen : tesla.Comp.SoundClose, tesla);
        _popup.PopupEntity(status ? Loc.GetString("tesla-coil-on") : Loc.GetString("tesla-coil-off"), tesla);
    }
}
