using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Server.Body.Systems;
using Content.Server.Maps;
using Content.Server.NodeContainer.EntitySystems;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
///     This is our SSAir equivalent, if you need to interact with or query atmos in any way, go through this.
/// </summary>
[UsedImplicitly]
[InjectDependencies]
public sealed partial class AtmosphereSystem : SharedAtmosphereSystem
{
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private InternalsSystem _internals = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private GasTileOverlaySystem _gasTileOverlaySystem = default!;
    [Dependency] private TransformSystem _transformSystem = default!;
    [Dependency] private TileSystem _tile = default!;

    private const float ExposedUpdateDelay = 1f;
    private float _exposedTimer = 0f;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(NodeGroupSystem));

        InitializeBreathTool();
        InitializeGases();
        InitializeCommands();
        InitializeCVars();
        InitializeGridAtmosphere();
        InitializeMap();


        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);

    }

    public override void Shutdown()
    {
        base.Shutdown();

        ShutdownCommands();
    }

    private void OnTileChanged(ref TileChangedEvent ev)
    {
        InvalidateTile(ev.NewTile.GridUid, ev.NewTile.GridIndices);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateProcessing(frameTime);
        UpdateHighPressure(frameTime);

        _exposedTimer += frameTime;

        if (_exposedTimer < ExposedUpdateDelay)
            return;

        foreach (var (exposed, transform) in EntityManager.EntityQuery<AtmosExposedComponent, TransformComponent>())
        {
            var air = GetContainingMixture(exposed.Owner, transform:transform);

            if (air == null)
                continue;

            var updateEvent = new AtmosExposedUpdateEvent(transform.Coordinates, air, transform);
            RaiseLocalEvent(exposed.Owner, ref updateEvent);
        }

        _exposedTimer -= ExposedUpdateDelay;
    }
}
