using System.Numerics;
using Content.Server.Gateway.Components;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Server.Salvage;
using Content.Shared.Dataset;
using Content.Shared.Movement.Components;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Physics;
using Content.Shared.Procedural;
using Content.Shared.Salvage;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Gateway.Systems;

/// <summary>
/// Generates gateway destinations regularly and indefinitely that can be chosen from.
/// </summary>
public sealed class GatewayGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly GatewaySystem _gateway = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    [ValidatePrototypeId<DatasetPrototype>]
    private const string PlanetNames = "names_borer";

    // TODO:
    // The destination maps get some kind of unlock
    // Whether a gateway is unlocked depends if its host map is unlocked.

    // Combine Gateway + GatewayDestination
    // Gateway takes the spawning gateway as the unlock timer
    // One-way portals

    // GATEWAY WINDOW
    // Mark any locked ones as locked until unlocked
    // If NextUnlock < curtime then undisable all locked ones (assuming NextReady is also up)
    // After taken then disable them all again

    // Need non-binary portals

    // Re-use salvage config stuff for the RNG
    // Also add weather coz it's funny.

    // Add songs (incl. the downloaded one) to the ambient music playlist for planet probably.
    // Add dungeon name to thing
    // Add biome template to thing
    // Add biome template options.
    // Copy most of salvage mission spawner
    // Add like a configs thing or the rng thing like salvage I guess.
    // Think of something for resources
    // Add mob templates like lizards or aliens or w/e
    // Probably need reduced ore spawn rate.
    // Fix unlocks / locking
    // Add an initial 15min lockout or something on roundstart

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GatewayGeneratorComponent, EntityUnpausedEvent>(OnGeneratorUnpaused);
        SubscribeLocalEvent<GatewayGeneratorComponent, MapInitEvent>(OnGeneratorMapInit);
        SubscribeLocalEvent<GatewayGeneratorDestinationComponent, AttemptGatewayOpenEvent>(OnGeneratorAttemptOpen);
        SubscribeLocalEvent<GatewayGeneratorDestinationComponent, GatewayOpenEvent>(OnGeneratorOpen);
    }

    private void OnGeneratorUnpaused(Entity<GatewayGeneratorComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.NextUnlock += args.PausedTime;
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
            _biome.EnsurePlanet(mapUid, _protoManager.Index<BiomeTemplatePrototype>("Continental"), seed);

            var grid = Comp<MapGridComponent>(mapUid);

            for (var x = -2; x <= 2; x++)
            {
                for (var y = -2; y <= 2; y++)
                {
                    tiles.Add((new Vector2i(x, y) + origin, new Tile(tileDef.TileId, variant: random.NextByte(tileDef.Variants))));
                }
            }

            // Clear area nearby as a sort of landing pad.
            _maps.SetTiles(mapUid, grid, tiles);

            var gatewayName = SharedSalvageSystem.GetFTLName(_protoManager.Index<DatasetPrototype>(PlanetNames), seed);

            _metadata.SetEntityName(mapUid, gatewayName);
            var originCoords = new EntityCoordinates(mapUid, origin);

            var gatewayUid = SpawnAtPosition("GatewayDestination", originCoords);
            var gatewayComp = Comp<GatewayDestinationComponent>(gatewayUid);
            _gateway.SetDestinationName(gatewayUid, FormattedMessage.FromMarkup($"[color=#D381C996]{gatewayName}[/color]"), gatewayComp);
            _gateway.SetEnabled(gatewayUid, true, gatewayComp);
            component.Generated.Add(mapUid);

            var genDest = AddComp<GatewayGeneratorDestinationComponent>(mapUid);
            genDest.Origin = origin;
            genDest.Seed = seed;
            genDest.Generator = uid;

            // Enclose the area
            var boundaryUid = Spawn(null, originCoords);
            var boundaryPhysics = AddComp<PhysicsComponent>(boundaryUid);
            var cShape = new ChainShape();
            // Don't need it to be a perfect circle, just need it to be loosely accurate.
            cShape.CreateLoop(Vector2.Zero, restriction.Range + 1f, false, count: 4);
            _fixtures.TryCreateFixture(
                boundaryUid,
                cShape,
                "boundary",
                collisionLayer: (int) (CollisionGroup.HighImpassable | CollisionGroup.Impassable | CollisionGroup.LowImpassable),
                body: boundaryPhysics);
            _physics.WakeBody(boundaryUid, body: boundaryPhysics);
            AddComp<BoundaryComponent>(boundaryUid);
        }

        Dirty(uid, component);
    }

    private void OnGeneratorAttemptOpen(Entity<GatewayGeneratorDestinationComponent> ent, ref AttemptGatewayOpenEvent args)
    {
        if (ent.Comp.Loaded || args.Cancelled)
            return;

        if (!TryComp(ent.Comp.Generator, out GatewayGeneratorComponent? generatorComp))
            return;

        if (generatorComp.NextUnlock + _metadata.GetPauseTime(ent.Owner) <= _timing.CurTime)
            return;

        args.Cancelled = true;
    }

    private void OnGeneratorOpen(Entity<GatewayGeneratorDestinationComponent> ent, ref GatewayOpenEvent args)
    {
        if (ent.Comp.Loaded)
            return;

        if (TryComp(ent.Comp.Generator, out GatewayGeneratorComponent? generatorComp))
        {
            generatorComp.NextUnlock = _timing.CurTime + generatorComp.UnlockCooldown;
            _gateway.UpdateAllGateways();
        }

        if (!TryComp(args.MapUid, out MapGridComponent? grid))
            return;

        ent.Comp.Loaded = true;
        var seed = ent.Comp.Seed;
        var origin = ent.Comp.Origin;
        var random = new Random(seed);
        var dungeonDistance = random.Next(3, 6);
        var dungeonRotation = _dungeon.GetDungeonRotation(seed);
        var dungeonPosition = origin + dungeonRotation.RotateVec(new Vector2i(0, dungeonDistance)).Floored();

        _dungeon.GenerateDungeon(_protoManager.Index<DungeonConfigPrototype>("Experiment"), args.MapUid, grid, dungeonPosition, seed);
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
    public TimeSpan UnlockCooldown = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Maps we've generated.
    /// </summary>
    [DataField]
    public List<EntityUid> Generated = new();
}

[RegisterComponent]
public sealed partial class GatewayGeneratorDestinationComponent : Component
{
    /// <summary>
    /// Generator that created this destination.
    /// </summary>
    [DataField]
    public EntityUid Generator;

    /// <summary>
    /// Is the map locked from being used still or unlocked.
    /// Used in conjunction with the attached generator's NextUnlock.
    /// </summary>
    [DataField]
    public bool Locked;

    [DataField]
    public bool Loaded;

    /// <summary>
    /// Seed used for this destination.
    /// </summary>
    [DataField]
    public int Seed;

    /// <summary>
    /// Origin of the gateway.
    /// </summary>
    [DataField]
    public Vector2i Origin;
}
