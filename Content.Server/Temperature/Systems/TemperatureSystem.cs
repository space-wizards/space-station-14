using Content.Server.Atmos.EntitySystems;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;

namespace Content.Server.Temperature.Systems;

public sealed class TemperatureSystem : SharedTemperatureSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemperatureComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);
    }

    private void OnAtmosExposedUpdate(Entity<TemperatureComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        var transform = args.Transform;
        if (transform.MapUid == null)
            return;

        TemperatureComponent temperature = ent;
        var temperatureDelta = args.GasMixture.Temperature - temperature.CurrentTemperature;
        var airHeatCapacity = _atmosphere.GetHeatCapacity(args.GasMixture, false);
        var heatCapacity = GetHeatCapacity((ent, ent, null));
        var heat = temperatureDelta * (airHeatCapacity * heatCapacity /
                                       (airHeatCapacity + heatCapacity));
        ChangeHeat((ent, ent), heat * temperature.AtmosTemperatureTransferEfficiency);
    }
}
