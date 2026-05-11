using Content.Server.Administration.Logs;
using Content.Server.Buckle.Systems;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Station.Systems;
using Content.Server.Stunnable;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Light.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Salvage;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Maps;

namespace Content.Server.Shuttles.Systems;

[UsedImplicitly]
public sealed partial class ShuttleSystem : SharedShuttleSystem
{
    [Dependency] private IAdminLogManager _logger = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private BiomeSystem _biomes = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private BuckleSystem _buckle = default!;
    [Dependency] private DamageableSystem _damageSys = default!;
    [Dependency] private DockingSystem _dockSystem = default!;
    [Dependency] private DungeonSystem _dungeon = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MapLoaderSystem _loader = default!;
    [Dependency] private MapSystem _mapSystem = default!;
    [Dependency] private MetaDataSystem _metadata = default!;
    [Dependency] private PvsOverrideSystem _pvs = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedSalvageSystem _salvage = default!;
    [Dependency] private ShuttleConsoleSystem _console = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private StunSystem _stuns = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private ThrusterSystem _thruster = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    [Dependency] private TurfSystem _turf = default!;

    private EntityQuery<BuckleComponent> _buckleQuery;
    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _buckleQuery = GetEntityQuery<BuckleComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        InitializeFTL();
        InitializeGridFills();
        InitializeIFF();
        InitializeImpact();

        SubscribeLocalEvent<ShuttleComponent, ComponentStartup>(OnShuttleStartup);
        SubscribeLocalEvent<ShuttleComponent, ComponentShutdown>(OnShuttleShutdown);
        SubscribeLocalEvent<ShuttleComponent, TileFrictionEvent>(OnTileFriction);
        SubscribeLocalEvent<ShuttleComponent, FTLStartedEvent>(OnFTLStarted);
        SubscribeLocalEvent<ShuttleComponent, FTLCompletedEvent>(OnFTLCompleted);

        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateHyperspace();
    }

    private void OnGridInit(GridInitializeEvent ev)
    {
        if (HasComp<MapComponent>(ev.EntityUid))
            return;

        EnsureComp<ShuttleComponent>(ev.EntityUid);

        // This and RoofComponent should be mutually exclusive, so ImplicitRoof should be removed if the grid has RoofComponent
        if (HasComp<RoofComponent>(ev.EntityUid))
            RemComp<ImplicitRoofComponent>(ev.EntityUid);
        else
            EnsureComp<ImplicitRoofComponent>(ev.EntityUid);
    }

    private void OnShuttleStartup(EntityUid uid, ShuttleComponent component, ComponentStartup args)
    {
        if (!HasComp<MapGridComponent>(uid))
        {
            return;
        }

        if (!TryComp(uid, out PhysicsComponent? physicsComponent))
        {
            return;
        }

        if (component.Enabled)
        {
            Enable(uid, component: physicsComponent, shuttle: component);
        }

        component.DampingModifier = component.BodyModifier;
    }

    public void Toggle(EntityUid uid, ShuttleComponent component)
    {
        if (!TryComp(uid, out PhysicsComponent? physicsComponent))
            return;

        component.Enabled = !component.Enabled;

        if (component.Enabled)
        {
            Enable(uid, component: physicsComponent, shuttle: component);
        }
        else
        {
            Disable(uid, component: physicsComponent);
        }
    }

    public void Enable(EntityUid uid, FixturesComponent? manager = null, PhysicsComponent? component = null, ShuttleComponent? shuttle = null)
    {
        if (!Resolve(uid, ref manager, ref component, ref shuttle, false))
            return;

        _physics.SetBodyType(uid, BodyType.Dynamic, manager: manager, body: component);
        _physics.SetBodyStatus(uid, component, BodyStatus.InAir);
        _physics.SetFixedRotation(uid, false, manager: manager, body: component);
    }

    public void Disable(EntityUid uid, FixturesComponent? manager = null, PhysicsComponent? component = null)
    {
        if (!Resolve(uid, ref manager, ref component, false))
            return;

        _physics.SetBodyType(uid, BodyType.Static, manager: manager, body: component);
        _physics.SetBodyStatus(uid, component, BodyStatus.OnGround);
        _physics.SetFixedRotation(uid, true, manager: manager, body: component);
    }

    private void OnShuttleShutdown(EntityUid uid, ShuttleComponent component, ComponentShutdown args)
    {
        // None of the below is necessary for any cleanup if we're just deleting.
        if (Comp<MetaDataComponent>(uid).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        Disable(uid);
    }

    private void OnTileFriction(Entity<ShuttleComponent> ent, ref TileFrictionEvent args)
    {
        args.Modifier *= ent.Comp.DampingModifier;
    }

    private void OnFTLStarted(Entity<ShuttleComponent> ent, ref FTLStartedEvent args)
    {
        ent.Comp.DampingModifier = 0f;
    }

    private void OnFTLCompleted(Entity<ShuttleComponent> ent, ref FTLCompletedEvent args)
    {
        ent.Comp.DampingModifier = ent.Comp.BodyModifier;
    }
}
