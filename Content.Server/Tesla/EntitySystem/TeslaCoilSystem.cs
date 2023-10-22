using Content.Server.Singularity.Components;
using Content.Shared.Interaction;
using Content.Shared.Singularity.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Radiation.Events;
using Robust.Shared.Timing;
using Robust.Shared.Containers;
using Content.Shared.Examine;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Atmos;
using Content.Server.Tesla.Components;
using Content.Server.Lightning;
using Content.Server.Power.EntitySystems;

namespace Content.Server.Tesla.EntitySystems;

public sealed class TeslaCoilSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeslaCoilComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<TeslaCoilComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TeslaCoilComponent, HittedByLightningEvent>(OnHittedLightning);
    }
    //
    private void OnMapInit(EntityUid uid, TeslaCoilComponent component, MapInitEvent args)
    {
        
    }

    //When interacting, turn the coil on or off.
    private void OnInteractHand(EntityUid uid, TeslaCoilComponent component, InteractHandEvent args)
    {
        ToggleCoil(uid, component, !component.Enabled);
    }

    //When struck by lightning, charge the internal battery
    private void OnHittedLightning(EntityUid uid, TeslaCoilComponent component, ref HittedByLightningEvent args)
    {
        Log.Debug("Попадание");
        if (!component.Enabled)
            return;
        if (!TryComp<BatteryComponent>(uid, out var batteryComponent))
            return;

        _battery.SetCharge(uid, batteryComponent.CurrentCharge + component.ChargeFromLightning);
        Log.Debug("Заряжено! " + batteryComponent.CurrentCharge);
    }

    private void ToggleCoil(EntityUid uid, TeslaCoilComponent component, bool status)
    {
        component.Enabled = status;
        //appearance
        //sound
        //popup
        Log.Debug("Статус: " + component.Enabled);
    }
}
