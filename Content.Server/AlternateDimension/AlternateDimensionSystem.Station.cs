using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Shared.AlternateDimension;
using Robust.Shared.Random;

namespace Content.Server.AlternateDimension;

public sealed partial class AlternateDimensionSystem
{
    private void InitializeStation()
    {
        SubscribeLocalEvent<StationAlternateDimensionGeneratorComponent, StationPostInitEvent>(OnStationInit);
    }

    private void OnStationInit(Entity<StationAlternateDimensionGeneratorComponent> ent, ref StationPostInitEvent args)
    {
        if (!TryComp<StationDataComponent>(ent, out var stationData))
            return;

        var alterParams = new AlternateDimensionParams
        {
            Seed = _random.Next(),
            Dimension = _random.Pick(ent.Comp.Dimensions),
        };

        var stationGrid = _stationSystem.GetLargestGrid(stationData);

        if (stationGrid is null)
            return;

        MakeAlternativeRealityGrid(stationGrid.Value, alterParams);
    }
}
