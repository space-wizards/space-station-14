using Content.Server.Body.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Systems;
using Content.Server.Stunnable;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Systems;
using Content.Shared.Shuttles.Systems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.Shuttles.Systems;

[UsedImplicitly]
[InjectDependencies]
public sealed partial class ShuttleSystem : SharedShuttleSystem
{
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private BodySystem _bobby = default!;
    [Dependency] private DockingSystem _dockSystem = default!;
    [Dependency] private DoorSystem _doors = default!;
    [Dependency] private DoorBoltSystem _bolts = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private FixtureSystem _fixtures = default!;
    [Dependency] private MapLoaderSystem _loader = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ShuttleConsoleSystem _console = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private StunSystem _stuns = default!;
    [Dependency] private ThrusterSystem _thruster = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private ISawmill _sawmill = default!;

    public const float TileMassMultiplier = 0.5f;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("shuttles");

        InitializeFTL();
        InitializeGridFills();
        InitializeIFF();
        InitializeImpact();

        SubscribeLocalEvent<ShuttleComponent, ComponentStartup>(OnShuttleStartup);
        SubscribeLocalEvent<ShuttleComponent, ComponentShutdown>(OnShuttleShutdown);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        SubscribeLocalEvent<FixturesComponent, GridFixtureChangeEvent>(OnGridFixtureChange);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ShutdownGridFills();
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateHyperspace(frameTime);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        CleanupHyperspace();
    }

    private void OnGridFixtureChange(EntityUid uid, FixturesComponent manager, GridFixtureChangeEvent args)
    {
        foreach (var fixture in args.NewFixtures)
        {
            _physics.SetDensity(uid, fixture.Key, fixture.Value, TileMassMultiplier, false, manager);
            _fixtures.SetRestitution(uid, fixture.Key, fixture.Value, 0.1f, false, manager);
        }
    }

    private void OnGridInit(GridInitializeEvent ev)
    {
        if (HasComp<MapComponent>(ev.EntityUid))
            return;

        EntityManager.EnsureComponent<ShuttleComponent>(ev.EntityUid);
    }

    private void OnShuttleStartup(EntityUid uid, ShuttleComponent component, ComponentStartup args)
    {
        if (!EntityManager.HasComponent<MapGridComponent>(uid))
        {
            return;
        }

        if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent))
        {
            return;
        }

        if (component.Enabled)
        {
            Enable(uid, physicsComponent, component);
        }
    }

    public void Toggle(EntityUid uid, ShuttleComponent component)
    {
        if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent))
            return;

        component.Enabled = !component.Enabled;

        if (component.Enabled)
        {
            Enable(uid, physicsComponent, component);
        }
        else
        {
            Disable(uid, physicsComponent);
        }
    }

    private void Enable(EntityUid uid, PhysicsComponent component, ShuttleComponent shuttle)
    {
        FixturesComponent? manager = null;

        _physics.SetBodyType(uid, BodyType.Dynamic, manager: manager, body: component);
        _physics.SetBodyStatus(component, BodyStatus.InAir);
        _physics.SetFixedRotation(uid, false, manager: manager, body: component);
        _physics.SetLinearDamping(component, shuttle.LinearDamping);
        _physics.SetAngularDamping(component, shuttle.AngularDamping);
    }

    private void Disable(EntityUid uid, PhysicsComponent component)
    {
        FixturesComponent? manager = null;

        _physics.SetBodyType(uid, BodyType.Static, manager: manager, body: component);
        _physics.SetBodyStatus(component, BodyStatus.OnGround);
        _physics.SetFixedRotation(uid, true, manager: manager, body: component);
    }

    private void OnShuttleShutdown(EntityUid uid, ShuttleComponent component, ComponentShutdown args)
    {
        // None of the below is necessary for any cleanup if we're just deleting.
        if (EntityManager.GetComponent<MetaDataComponent>(uid).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent))
        {
            return;
        }

        Disable(uid, physicsComponent);
    }
}
