using Content.Server.Parallax;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Station.Systems;
public sealed partial class StationBiomeSystem : EntitySystem
{
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationBiomeComponent, StationPostInitEvent>(OnStationPostInit);
    }

    private void OnStationPostInit(Entity<StationBiomeComponent> map, ref StationPostInitEvent args)
    {
        if (!TryComp(map, out StationDataComponent? dataComp))
            return;

        var station = _station.GetLargestGrid(dataComp);
        if (station == null) return;

        var mapId = Transform(station.Value).MapID;
        var mapUid = _mapManager.GetMapEntityId(mapId);

        _biome.EnsurePlanet(mapUid, _proto.Index(map.Comp.Biome), map.Comp.Seed, mapLight: map.Comp.MapLightColor);
    }
}
