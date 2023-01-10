using Content.Server.Shuttles.Components;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Shuttles.Systems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Shuttles.Systems
{
    [UsedImplicitly]
    public sealed partial class ShuttleSystem : SharedShuttleSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly FixtureSystem _fixtures = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        private ISawmill _sawmill = default!;

        public const float TileMassMultiplier = 0.5f;

        public const float ShuttleLinearDamping = 0.05f;
        public const float ShuttleAngularDamping = 0.05f;

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = Logger.GetSawmill("shuttles");

            InitializeEmergencyConsole();
            InitializeEscape();
            InitializeFTL();
            InitializeIFF();
            InitializeImpact();

            SubscribeLocalEvent<ShuttleComponent, ComponentAdd>(OnShuttleAdd);
            SubscribeLocalEvent<ShuttleComponent, ComponentStartup>(OnShuttleStartup);
            SubscribeLocalEvent<ShuttleComponent, ComponentShutdown>(OnShuttleShutdown);

            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

            SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
            SubscribeLocalEvent<GridFixtureChangeEvent>(OnGridFixtureChange);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            UpdateEmergencyConsole(frameTime);
            UpdateHyperspace(frameTime);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent ev)
        {
            CleanupEmergencyConsole();
            CleanupEmergencyShuttle();
            CleanupHyperspace();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            ShutdownEscape();
            ShutdownEmergencyConsole();
        }

        private void OnShuttleAdd(EntityUid uid, ShuttleComponent component, ComponentAdd args)
        {
            // Easier than doing it in the comp and they don't have constructors.
            for (var i = 0; i < component.LinearThrusters.Length; i++)
            {
                component.LinearThrusters[i] = new List<ThrusterComponent>();
            }
        }

        private void OnGridFixtureChange(GridFixtureChangeEvent args)
        {
            // Look this is jank but it's a placeholder until we design it.
            if (args.NewFixtures.Count == 0) return;

            var manager = Comp<FixturesComponent>(args.NewFixtures[0].Body.Owner);

            foreach (var fixture in args.NewFixtures)
            {
                _physics.SetDensity(fixture, TileMassMultiplier, manager, false);
                _fixtures.SetRestitution(fixture, 0.1f, manager, false);
            }

            _fixtures.FixtureUpdate(manager, args.NewFixtures[0].Body);
        }

        private void OnGridInit(GridInitializeEvent ev)
        {
            if (HasComp<MapComponent>(ev.EntityUid))
                return;

            EntityManager.EnsureComponent<ShuttleComponent>(ev.EntityUid);
        }

        private void OnShuttleStartup(EntityUid uid, ShuttleComponent component, ComponentStartup args)
        {
            if (!EntityManager.HasComponent<MapGridComponent>(component.Owner))
            {
                return;
            }

            if (!EntityManager.TryGetComponent(component.Owner, out PhysicsComponent? physicsComponent))
            {
                return;
            }

            if (component.Enabled)
            {
                Enable(physicsComponent);
            }
        }

        public void Toggle(ShuttleComponent component)
        {
            if (!EntityManager.TryGetComponent(component.Owner, out PhysicsComponent? physicsComponent)) return;

            component.Enabled = !component.Enabled;

            if (component.Enabled)
            {
                Enable(physicsComponent);
            }
            else
            {
                Disable(physicsComponent);
            }
        }

        private void Enable(PhysicsComponent component)
        {
            component.BodyType = BodyType.Dynamic;
            component.BodyStatus = BodyStatus.InAir;
            component.FixedRotation = false;
            component.LinearDamping = ShuttleLinearDamping;
            component.AngularDamping = ShuttleAngularDamping;
        }

        private void Disable(PhysicsComponent component)
        {
            component.BodyType = BodyType.Static;
            component.BodyStatus = BodyStatus.OnGround;
            component.FixedRotation = true;
        }

        private void OnShuttleShutdown(EntityUid uid, ShuttleComponent component, ComponentShutdown args)
        {
            // None of the below is necessary for any cleanup if we're just deleting.
            if (EntityManager.GetComponent<MetaDataComponent>(uid).EntityLifeStage >= EntityLifeStage.Terminating) return;

            if (!EntityManager.TryGetComponent(component.Owner, out PhysicsComponent? physicsComponent))
            {
                return;
            }

            Disable(physicsComponent);
        }
    }
}
