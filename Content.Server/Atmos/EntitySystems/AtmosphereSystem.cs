using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Decals;
using Content.Shared.Doors.Components;
using Content.Shared.Maps;
using Content.Shared.Radiation.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Spawners;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using System.Linq;
using Content.Shared.Damage;
using Robust.Shared.Threading;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
///     This is our SSAir equivalent, if you need to interact with or query atmos in any way, go through this.
/// </summary>
[UsedImplicitly]
public sealed partial class AtmosphereSystem : SharedAtmosphereSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IParallelManager _parallel = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly GasTileOverlaySystem _gasTileOverlaySystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] public readonly PuddleSystem Puddle = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly TritiumRadiationSourceSystem _tritiumRadSystem = default!;
    [Dependency] private readonly TimedDespawnSystem _timedDespawnSystem = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    
    private const float ExposedUpdateDelay = 1f;
    private float _exposedTimer = 0f;

    private EntityQuery<GridAtmosphereComponent> _atmosQuery;
    private EntityQuery<MapAtmosphereComponent> _mapAtmosQuery;
    private EntityQuery<AirtightComponent> _airtightQuery;
    private EntityQuery<FirelockComponent> _firelockQuery;
    private HashSet<EntityUid> _entSet = new();

    private string[] _burntDecals = [];

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(NodeGroupSystem));

        InitializeGases();
        InitializeCommands();
        InitializeCVars();
        InitializeGridAtmosphere();
        InitializeMap();

        _mapAtmosQuery = GetEntityQuery<MapAtmosphereComponent>();
        _atmosQuery = GetEntityQuery<GridAtmosphereComponent>();
        _airtightQuery = GetEntityQuery<AirtightComponent>();
        _firelockQuery = GetEntityQuery<FirelockComponent>();

        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        CacheDecals();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        ShutdownCommands();
    }

    private void OnTileChanged(ref TileChangedEvent ev)
    {
        foreach (var change in ev.Changes)
        {
            InvalidateTile(ev.Entity.Owner, change.GridIndices);
        }
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<DecalPrototype>())
            CacheDecals();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateProcessing(frameTime);
        UpdateHighPressure(frameTime);

        _exposedTimer += frameTime;

        if (_exposedTimer < ExposedUpdateDelay)
            return;

        var query = EntityQueryEnumerator<AtmosExposedComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var transform))
        {
            var air = GetContainingMixture((uid, transform));

            if (air == null)
                continue;

            var updateEvent = new AtmosExposedUpdateEvent(transform.Coordinates, air, transform);
            RaiseLocalEvent(uid, ref updateEvent);
        }

        _exposedTimer -= ExposedUpdateDelay;
    }

    private void CacheDecals()
    {
        _burntDecals = _protoMan.EnumeratePrototypes<DecalPrototype>().Where(x => x.Tags.Contains("burnt")).Select(x => x.ID).ToArray();
    }

    public void ActivateTritiumFire(IGasMixtureHolder? holder, float burnedFuel)
    {
        if (holder == null || burnedFuel <= 0)
            return;

        if (holder is PipeNet pipeNet) // Catch pipes
        {
            if (burnedFuel > 0)
            {
                foreach (var node in pipeNet.Nodes)
                {
                    var radSource = EnsureComp<RadiationSourceComponent>(node.Owner);
                    var timer = EnsureComp<TritiumRadiationSourceComponent>(node.Owner);

                    radSource.Intensity = burnedFuel * Atmospherics.TritiumRadiationFactor / pipeNet.NodeCount;
                    radSource.Slope = 1.0f;
                    timer.Lifetime = 2.0f;
                }
            }
            return;
        }

        if (holder is IComponent component) // Catch canisters, tanks, etc
        {
            var radSource = EnsureComp<RadiationSourceComponent>(component.Owner);
            var timer = EnsureComp<TritiumRadiationSourceComponent>(component.Owner);

            radSource.Intensity = burnedFuel * Atmospherics.TritiumRadiationFactor;
            radSource.Slope = 1.0f;
            timer.Lifetime = 2.0f;
            return;
        }

        if (holder is TileAtmosphere tile) // Catch tiles
        {
            if (!_entityManager.EntityExists(tile.RadiationSource))
            {
                var coords = _mapSystem.ToCenterCoordinates(tile.GridIndex, tile.GridIndices);
                tile.RadiationSource = _entityManager.SpawnEntity(null, coords);
            }

            var radSource = EnsureComp<RadiationSourceComponent>(tile.RadiationSource.Value);
            radSource.Intensity = burnedFuel * Atmospherics.TritiumRadiationFactor;
            radSource.Slope = 1.0f;

            var timedDespawn = EnsureComp<TimedDespawnComponent>(tile.RadiationSource.Value);
            timedDespawn.Lifetime = 2f;
        }
    }
}
