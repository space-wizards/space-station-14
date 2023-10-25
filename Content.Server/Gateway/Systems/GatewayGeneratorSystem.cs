using Content.Server.Gateway.Components;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Server.Salvage;
using Content.Shared.Dataset;
using Content.Shared.Procedural;
using Content.Shared.Salvage;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

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
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly GatewaySystem _gateway = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SalvageSystem _salvage = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;

    [ValidatePrototypeId<DatasetPrototype>]
    private const string PlanetNames = "names_borer";

    // TODO:

    // TODO: Make the stencil texture static or something idk, some way to share it. Maybe add it to ParallaxSystem
    // Move the sprite thingie and also make gas tile overlay use the manual sprite drawing
    // Move parallax over to ParallaxSystem to draw sprites as parallax.

    // TODO: Portals to avoid people being stranded
    // Use fog instead of blackness for barrier.
    // Add songs (incl. the downloaded one) to the ambient music playlist for planet probably.
    // Add dungeon name to thing
    // Defer spawning for performance.
    // Add biome template to thing
    // Add biome template options.
    // Make continental bigger areas probably?
    // Think of something for resources
    // Fix unlocks / locking
    // Make a planet helper instead of calling command
    // Spawn dungeon / make an await for dungeons or something
    // apply biome templates
    // Add mob templates like lizards or aliens or w/e
    // Probably need reduced ore spawn rate.
    // Add an initial 15min lockout or something on roundstart
    // Need chain shape for barrier.

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GatewayGeneratorComponent, MapInitEvent>(OnGeneratorMapInit);
    }

    private void OnGeneratorMapInit(EntityUid uid, GatewayGeneratorComponent component, MapInitEvent args)
    {
        var tiles = new List<(Vector2i Index, Tile Tile)>();
        var tileDef = _tileDefManager["FloorSteel"];
        const int MaxOffset = 256;

        for (var i = 0; i < 3; i++)
        {
            tiles.Clear();
            var seed = _random.Next();
            var random = new Random(seed);
            var mapId = _mapManager.CreateMap();
            var mapUid = _mapManager.GetMapEntityId(mapId);
            var origin = new Vector2i(random.Next(-MaxOffset, MaxOffset), random.Next(-MaxOffset, MaxOffset));
            var restriction = AddComp<RestrictedRangeComponent>(mapUid);
            restriction.Origin = origin;
            // TODO: Not this.
            _console.ExecuteCommand($"planet {mapId} Continental");

            var grid = Comp<MapGridComponent>(mapUid);

            for (var x = -2; x <= 2; x++)
            {
                for (var y = -2; y <= 2; y++)
                {
                    tiles.Add((new Vector2i(x, y) + origin, new Tile(tileDef.TileId, variant: _random.NextByte(tileDef.Variants))));
                }
            }

            // Clear area nearby as a sort of landing pad.
            _maps.SetTiles(mapUid, grid, tiles);

            var gatewayName = SharedSalvageSystem.GetFTLName(_protoManager.Index<DatasetPrototype>(PlanetNames), seed);

            _metadata.SetEntityName(mapUid, gatewayName);

            var gatewayUid = SpawnAtPosition("GatewayDestination", new EntityCoordinates(mapUid, origin));
            var gatewayComp = Comp<GatewayDestinationComponent>(gatewayUid);
            _gateway.SetDestinationName(gatewayUid, FormattedMessage.FromMarkup($"[color=#D381C996]{gatewayName}[/color]"), gatewayComp);
            _gateway.SetEnabled(gatewayUid, true, gatewayComp);
            component.Generated.Add(mapUid);

            var dungeonDistance = random.Next(6, 12);
            var dungeonRotation = _dungeon.GetDungeonRotation(seed);
            var dungeonPosition = origin + (dungeonRotation.RotateVec(new Vector2i(0, dungeonDistance))).Floored();

            _dungeon.GenerateDungeon(_protoManager.Index<DungeonConfigPrototype>("Experiment"), mapUid, grid, dungeonPosition, seed);
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
