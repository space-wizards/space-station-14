// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Numerics;
using System.Threading.Tasks;
using Content.Server.Atmos.EntitySystems;
using Content.Server.DeadSpace.StationDnd.Components;
using Content.Server.GameTicking.Events;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Shared.Atmos;
using Content.Shared.Dataset;
using Content.Shared.DeadSpace.StationDnd;
using Content.Shared.GameTicking;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Physics;
using Content.Shared.Procedural;
using Content.Shared.Salvage;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.DeadSpace.StationDnd;

public sealed class StationDndSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly BiomeSystem _biomeSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly DungeonSystem _dungeonSystem = default!;
    [Dependency] private readonly SharedSalvageSystem _salvage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
        SubscribeLocalEvent<StationDndComponent, MapInitEvent>(OnStationInit);
        SubscribeLocalEvent<StationDndComponent, ComponentShutdown>(OnStationShutdown);
    }

    // Раунд константы
    public Entity<StationDndComponent>? SourceStation;
    public Entity<MapGridComponent>? DndMap;
    public MapId? DndMapId;
    public DndPrototype? CurrentSetting;

    [ViewVariables]
    public List<List<Dungeon>> Structures = new();

    [ViewVariables]
    public List<Dungeon>? BaseSpawn;

    private void EnsureMapLoaded()
    {
        if (DndMap != null)
            return;

        Log.Info("EnsureMapLoaded");

        var mapUid = _map.CreateMap(out var mapId, false);
        var planetGrid = EnsureComp<MapGridComponent>(mapUid);
        DndMap = (mapUid, planetGrid);
        DndMapId = mapId;
    }

    #region PlanetSeed
    private void SetMapPlanet()
    {
        if (DndMap == null || TerminatingOrDeleted(DndMap.Value) || DndMapId == null)
        {
            Log.Error("Ошибка! Пропущен вызов EnsureMapLoaded! Пропуск!");
            return;
        }
        if (CurrentSetting == null)
        {
            Log.Error("Ошибка! Пропущен компонент ссылки на станцию! Пропуск!");
            return;
        }
        if (HasComp<BiomeComponent>(DndMap))
        {
            Log.Error("Ошибка! Планета уже сгенерирована! Пропуск!");
            return;
        }

        var seed = _random.Next();

        Entity<BiomeComponent> biome = (DndMap.Value, AddComp<BiomeComponent>(DndMap.Value));
        _biomeSystem.SetSeed(biome, biome, seed, false);
        _biomeSystem.SetTemplate(biome, biome, _prototype.Index(CurrentSetting.BiomePrototype), false);
        foreach (var layer in CurrentSetting.LootLayers)
        {
            _biomeSystem.AddMarkerLayer(biome, biome, layer);
        }
        _biomeSystem.AddTemplate(biome, biome, "Loot", _prototype.Index<BiomeTemplatePrototype>("Caves"), 1);
        Dirty(biome);

        // Gravity
        if (CurrentSetting.Gravity)
        {
            var gravity = EnsureComp<GravityComponent>(biome);
            gravity.Enabled = true;
            Dirty(biome, gravity);
        }

        // Atmos
        if (CurrentSetting.Atmosphere != null)
        {
            _atmosphereSystem.SetMapAtmosphere(biome, false, CurrentSetting.Atmosphere);
        }
        else
        {
            // Some very generic default breathable atmosphere.
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int) Gas.Oxygen] = 21.824779f;
            moles[(int) Gas.Nitrogen] = 82.10312f;

            var mixture = new GasMixture(moles, 293.15f,2500);

            _atmosphereSystem.SetMapAtmosphere(biome, false, mixture);
        }

        // Lighting
        if (CurrentSetting.LightColor != null)
        {
            var lighting = EnsureComp<MapLightComponent>(biome);
            lighting.AmbientLightColor = CurrentSetting.LightColor.Value;
            Dirty(biome, lighting);
        }

        // planetName
        var planetName = _salvage.GetFTLName(_prototype.Index<LocalizedDatasetPrototype>(CurrentSetting.NameDataset), seed);
        _metadata.SetEntityName(biome, planetName);

        // Позиция карта (точка начала)
        var mapPos = new MapCoordinates(new Vector2(0f, 0f), DndMapId.Value);

        var restriction = AddComp<RestrictedRangeComponent>(biome);
        restriction.Origin = mapPos.Position;
        restriction.Range = CurrentSetting.MapMaxDistance;

        // Enclose the area
        var boundaryUid = Spawn(null, mapPos);
        var boundaryPhysics = AddComp<PhysicsComponent>(boundaryUid);
        var cShape = new ChainShape();
        // Don't need it to be a perfect circle, just need it to be loosely accurate.
        cShape.CreateLoop(Vector2.Zero, restriction.Range + 1f, false, count: 4);
        _fixture
            .TryCreateFixture(
                boundaryUid,
                cShape,
                "boundary",
                collisionLayer: (int) (CollisionGroup.HighImpassable | CollisionGroup.Impassable | CollisionGroup.LowImpassable | CollisionGroup.GhostImpassable),
                body: boundaryPhysics);
        _physics.WakeBody(boundaryUid, body: boundaryPhysics);
        AddComp<BoundaryComponent>(boundaryUid);

        _mapManager.DoMapInitialize(DndMapId.Value);

        var preloadArea = new Vector2(CurrentSetting.MapMaxDistance, CurrentSetting.MapMaxDistance);
        var targetArea = new Box2(mapPos.Position - preloadArea, mapPos.Position + preloadArea);
        Log.Info("PreloadPlanet {0}", targetArea.ToString());
        _biomeSystem.Preload(biome, biome, targetArea);
        _mapManager.SetMapPaused(DndMapId.Value, true);
        Log.Info("PreloadPlanet DONE!");
    }

    private async Task SpawnPlanetaryStructures()
    {
        if (DndMap == null || TerminatingOrDeleted(DndMap.Value) || DndMapId == null)
        {
            Log.Error("Ошибка! Пропущен вызов EnsureMapLoaded! Пропуск!");
            return;
        }
        if (CurrentSetting == null || SourceStation == null)
        {
            Log.Error("Ошибка! Пропущен компонент ссылки на станцию! Пропуск!");
            return;
        }
        if (BaseSpawn != null)
        {
            Log.Error("Ошибка! Планета уже заполнена! Пропуск!");
            return;
        }

        var origin = new EntityCoordinates(DndMap.Value, Vector2.Zero);
        var directions = new Vector2i[]
        {
            ( 0,  1),
            ( 1,  1),
            ( 1,  0),
            ( 0, -1),
            (-1, -1),
            (-1,  0),
            ( 1, -1),
            (-1,  1),
        };

        var structuresToBuild = new List<DungeonConfigPrototype>();
        foreach (var (dungeon, count) in CurrentSetting.Structures)
        {
            var dungeonProto = _prototype.Index<DungeonConfigPrototype>(dungeon);

            for (var i = 0; i < count; ++i)
            {
                structuresToBuild.Add(dungeonProto);
            }
        }

        _random.Shuffle(structuresToBuild);
        _random.Shuffle(directions);

        var minDistance = CurrentSetting.MinStructureDistance;
        var maxDistance = CurrentSetting.MaxStructureDistance;

        foreach (var direction in directions)
        {
            var distance = Math.Min(_random.Next(minDistance, (int) (minDistance * 1.2)), maxDistance - 15);

            var point = direction * distance;

            var dungeonProto = structuresToBuild.Pop();
            var dungeon = await _dungeonSystem.GenerateDungeonAsync(dungeonProto,
                DndMap.Value,
                DndMap.Value,
                point,
                _random.Next());

            Structures.Add(dungeon);
        }

        BaseSpawn = await _dungeonSystem.GenerateDungeonAsync(_prototype.Index(CurrentSetting.SpawnBase),
             DndMap.Value,
             DndMap.Value,
             Vector2i.Zero,
             _random.Next());
    }
    #endregion

    private void OnRoundStart(RoundStartingEvent ev)
    {
        EnsureMapLoaded();
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
    {
        SourceStation = null;
        DndMap = null;
        DndMapId = null;
        CurrentSetting = null;
        Structures.Clear();
        BaseSpawn = null;
    }

    private void OnStationShutdown(Entity<StationDndComponent> ent, ref ComponentShutdown args)
    {

    }

    private async void OnStationInit(EntityUid entityUid, StationDndComponent component, MapInitEvent args)
    {
        if (CurrentSetting != null)
        {
            Log.Warning("2 станции? попытка загрузки второго мира ДнД? Пропуск!");
            return;
        }

        EnsureMapLoaded();
        CurrentSetting = _prototype.Index(_random.Pick(component.Configs));
        SourceStation = (entityUid, component);

        Log.Info("Выбран мир ДнД: {0}", CurrentSetting.ID);

        SetMapPlanet();
        await SpawnPlanetaryStructures();

        if (DndMapId.HasValue)
            _mapManager.SetMapPaused(DndMapId.Value, false);
    }

}
