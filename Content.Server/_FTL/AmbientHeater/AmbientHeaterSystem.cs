using System.Linq;
using Content.Server._FTL.AmbientHeater;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Temperature.Systems;
using Robust.Server.GameObjects;

namespace Content.Server._Frigid.AmbientHeater;

public sealed class AmbientHeaterSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AmbientHeaterComponent, PowerChangedEvent>(HandlePowerChange);
    }

    private void HandlePowerChange(EntityUid uid, AmbientHeaterComponent component, ref PowerChangedEvent args)
    {
        component.Powered = args.Powered;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (heater, transform) in EntityQuery<AmbientHeaterComponent, TransformComponent>())
        {
            if (heater.Powered == false && heater.RequiresPower)
                continue;

            var ent = transform.ParentUid;
            var grid = transform.GridUid;
            var map = transform.MapUid;
            var indices = _transformSystem.GetGridOrMapTilePosition(ent, transform);
            var mixture = _atmosphereSystem.GetTileMixture(grid, map, indices, true);
            if (mixture is { })
            {
                if (mixture.Temperature <= heater.TargetTemperature)
                    mixture.Temperature += heater.HeatPerSecond * frameTime;
            }
        }
    }
}
