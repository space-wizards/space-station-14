using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Temperature.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Temperature.Systems;

public sealed class RadiantTemperatureSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadiantTemperatureComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
    }

    private void OnAtmosUpdate(Entity<RadiantTemperatureComponent> entity, ref AtmosDeviceUpdateEvent args)
    {
        var entXform = Transform(entity);
        var grid = entXform.GridUid;
        var map = entXform.MapUid;
        var indices = _xform.GetGridTilePositionOrDefault((entity, entXform));
        var mixture = _atmosphere.GetTileMixture(grid, map, indices, true);

        if (mixture is null)
            return;

        // do not continue heating if air is hotter than goal temperature,
        // and the entity is a radiant HEAT source (positive temp changes)
        if (mixture.Temperature > entity.Comp.GoalTemperature && entity.Comp.EnergyChangedPerSecond > 0)
            return;

        // do not continue cooling if air is colder than goal temperature
        // and the entity is a radiant COOLING source (negative temp changes)
        if (mixture.Temperature < entity.Comp.GoalTemperature && entity.Comp.EnergyChangedPerSecond < 0)
            return;

        var dQ = entity.Comp.EnergyChangedPerSecond * args.dt;

        // Clamps the heat transferred to not overshoot
        // This is just taken straight from GasThermoMachineSystem.cs
        var Cin = _atmosphere.GetHeatCapacity(mixture, true);
        var dT = entity.Comp.GoalTemperature - mixture.Temperature;
        var dQLim = dT * Cin;
        var scale = 1f;
        if (Math.Abs(dQ) > Math.Abs(dQLim))
        {
            scale = dQLim / dQ;
        }

        var dQActual = dQ * scale;
        _atmosphere.AddHeat(mixture, dQActual);

    }
}
