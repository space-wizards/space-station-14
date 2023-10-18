using System.Numerics;
using Content.Server.Gateway.Components;
using Content.Server.Parallax;
using Content.Server.Salvage;
using Content.Shared.Salvage;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Gateway.Systems;

/// <summary>
/// Generates gateway destinations regularly and indefinitely that can be chosen from.
/// </summary>
public sealed class GatewayGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly GatewaySystem _gateway = default!;
    [Dependency] private readonly SalvageSystem _salvage = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GatewayGeneratorComponent, MapInitEvent>(OnGeneratorMapInit);
    }

    private void OnGeneratorMapInit(EntityUid uid, GatewayGeneratorComponent component, MapInitEvent args)
    {
        var loadRadius = 8;
        var tiles = new List<(Vector2i Index, Tile Tile)>();
        var tileDef = _tileDefManager["FloorSteel"];

        for (var x = -loadRadius; x < loadRadius; x++)
        {
            for (var y = -loadRadius; y < loadRadius; y++)
            {
                tiles.Add((new Vector2i(x, y), new Tile(tileDef.TileId, variant: _random.NextByte(tileDef.Variants))));
            }
        }

        for (var i = 0; i < 3; i++)
        {
            var mapId = _mapManager.CreateMap();
            var mapUid = _mapManager.GetMapEntityId(mapId);
            // TODO: Not this.
            _console.ExecuteCommand($"planet {mapId} Continental");

            var grid = Comp<MapGridComponent>(mapUid);
            _maps.SetTiles(mapUid, grid, tiles);
            var gatewayUid = SpawnAtPosition("GatewayDestination", new EntityCoordinates(mapUid, Vector2.Zero));
            var gatewayComp = Comp<GatewayDestinationComponent>(gatewayUid);
            _gateway.SetEnabled(gatewayUid, true, gatewayComp);
            AddComp<RestrictedRangeComponent>(mapUid);
            component.Generated.Add(mapUid);
        }

        Dirty(uid, component);
    }
}

/// <summary>
/// Generates gateway destinations at a regular interval.
/// </summary>
[RegisterComponent]
public sealed partial class GatewayGeneratorComponent : Component
{
    /// <summary>
    /// Next time another seed unlocks.
    /// </summary>
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextUnlock;

    /// <summary>
    /// How long it takes to unlock another destination once one is taken.
    /// </summary>
    [DataField]
    public TimeSpan UnlockCooldown = TimeSpan.FromMinutes(45);

    /// <summary>
    /// Maps we've generated.
    /// </summary>
    [DataField]
    public List<EntityUid> Generated = new();
}
