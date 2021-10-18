using Content.Server.Atmos.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Atmos.EntitySystems
{
    public class AtmosExposedSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly TemperatureSystem _temperatureSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<AtmosExposedComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);
        }

        private void OnAtmosExposedUpdate(EntityUid uid, AtmosExposedComponent component, AtmosExposedUpdateEvent args)
        {
            if (EntityManager.TryGetComponent<TemperatureComponent>(uid, out var temperature))
            {
                var temperatureDelta = args.GasMixture.Temperature - temperature.CurrentTemperature;
                var tileHeatCapacity = _atmosphereSystem.GetHeatCapacity(args.GasMixture);
                var heat = temperatureDelta * (tileHeatCapacity * temperature.HeatCapacity / (tileHeatCapacity + temperature.HeatCapacity));
                _temperatureSystem.ReceiveHeat(uid, heat);
            }
        }
    }

    public class AtmosExposedUpdateEvent : EntityEventArgs
    {
        public EntityCoordinates Coordinates;
        public GasMixture GasMixture;

        public AtmosExposedUpdateEvent(EntityCoordinates coordinates, GasMixture mixture)
        {
            Coordinates = coordinates;
            GasMixture = mixture;
        }
    }
}
