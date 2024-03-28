using Content.Server.Parallax;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Parallax;
public sealed partial class SpawnMapBiomeSystem : EntitySystem
{
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpawnMapBiomeComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<SpawnMapBiomeComponent> map, ref MapInitEvent args)
    {
        if (!_mapManager.IsMap(map)) return;

        _biome.EnsurePlanet(map, _proto.Index(map.Comp.Biome));
    }
}
