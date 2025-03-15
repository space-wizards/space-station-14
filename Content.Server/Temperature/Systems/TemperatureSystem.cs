using Content.Server.Atmos.EntitySystems;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;

namespace Content.Server.Temperature.Systems;

public sealed class TemperatureSystem : SharedTemperatureSystem
{
    [Dependency] private readonly AtmosphereSystem _serverAtmos = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TemperatureComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);
    }

    private void OnAtmosExposedUpdate(EntityUid uid, TemperatureComponent temperature,
        ref AtmosExposedUpdateEvent args)
    {
        var transform = args.Transform;
        if (transform.MapUid == null)
            return;

        var temperatureDelta = args.GasMixture.Temperature - temperature.CurrentTemperature;
        var airHeatCapacity = _serverAtmos.GetHeatCapacity(args.GasMixture, false);
        var heatCapacity = GetHeatCapacity(uid, temperature);
        var heat = temperatureDelta * (airHeatCapacity * heatCapacity /
                                       (airHeatCapacity + heatCapacity));
        ChangeHeat(uid, heat * temperature.AtmosTemperatureTransferEfficiency, temperature: temperature);
    }
}
