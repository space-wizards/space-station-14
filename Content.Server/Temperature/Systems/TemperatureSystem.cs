using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Inventory;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;

namespace Content.Server.Temperature.Systems;

/// <inheritdoc/>
public sealed class TemperatureSystem : SharedTemperatureSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemperatureComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);
        SubscribeLocalEvent<TemperatureProtectionComponent, InventoryRelayedEvent<ModifyChangedTemperatureEvent>>(OnTemperatureChangeAttempt);

        SubscribeLocalEvent<InternalTemperatureComponent, MapInitEvent>(OnInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // conduct heat from the surface to the inside of entities with internal temperatures
        var query = EntityQueryEnumerator<InternalTemperatureComponent, TemperatureComponent>();
        while (query.MoveNext(out var uid, out var comp, out var temp))
        {
            // don't do anything if they equalised
            var diff = Math.Abs(temp.CurrentTemperature - comp.Temperature);
            if (diff < 0.1f)
                continue;

            // heat flow in W/m^2 as per fourier's law in 1D.
            var q = comp.Conductivity * diff / comp.Thickness;

            // convert to J then K
            var joules = q * comp.Area * frameTime;
            var degrees = joules / GetHeatCapacity((uid, temp));
            if (temp.CurrentTemperature < comp.Temperature)
                degrees *= -1;

            // exchange heat between inside and surface
            comp.Temperature += degrees;
            SetTemperature((uid, temp), temp.CurrentTemperature - degrees);
        }
    }

    private void OnAtmosExposedUpdate(EntityUid uid, TemperatureComponent temperature,
        ref AtmosExposedUpdateEvent args)
    {
        var transform = args.Transform;

        if (transform.MapUid == null)
            return;

        var temperatureDelta = args.GasMixture.Temperature - temperature.CurrentTemperature;
        var airHeatCapacity = _atmosphere.GetHeatCapacity(args.GasMixture, false);
        var heatCapacity = GetHeatCapacity((uid, temperature));
        var heat = temperatureDelta * (airHeatCapacity * heatCapacity /
                                       (airHeatCapacity + heatCapacity));
        AdjustThermalEnergy((uid, temperature), heat * temperature.AtmosTemperatureTransferEfficiency);
    }

    private void OnInit(EntityUid uid, InternalTemperatureComponent comp, MapInitEvent args)
    {
        if (!TryComp<TemperatureComponent>(uid, out var temp))
            return;

        comp.Temperature = temp.CurrentTemperature;
    }

    private void OnTemperatureChangeAttempt(EntityUid uid, TemperatureProtectionComponent component,
        InventoryRelayedEvent<ModifyChangedTemperatureEvent> args)
    {
        var ev = new GetTemperatureProtectionEvent(component.Coefficient);
        RaiseLocalEvent(uid, ref ev);

        args.Args.TemperatureDelta *= ev.Coefficient;
    }
}
