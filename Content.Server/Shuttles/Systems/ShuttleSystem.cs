using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Shuttles.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.Server.Shuttles.Systems
{
    [UsedImplicitly]
    public sealed partial class ShuttleSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly FixtureSystem _fixtures = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        private ISawmill _sawmill = default!;

        public const float TileMassMultiplier = 0.5f;

        public float ShuttleMaxLinearSpeed;

        public float ShuttleMaxAngularMomentum;
        public float ShuttleMaxAngularAcc;
        public float ShuttleMaxAngularSpeed;

        public float ShuttleIdleLinearDamping;
        public float ShuttleIdleAngularDamping;

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = Logger.GetSawmill("shuttles");

            InitializeEmergencyConsole();
            InitializeEscape();

            SubscribeLocalEvent<ShuttleComponent, ComponentAdd>(OnShuttleAdd);
            SubscribeLocalEvent<ShuttleComponent, ComponentStartup>(OnShuttleStartup);
            SubscribeLocalEvent<ShuttleComponent, ComponentShutdown>(OnShuttleShutdown);

            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

            SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
            SubscribeLocalEvent<GridFixtureChangeEvent>(OnGridFixtureChange);

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CCVars.ShuttleMaxLinearSpeed, SetShuttleMaxLinearSpeed, true);
            configManager.OnValueChanged(CCVars.ShuttleMaxAngularSpeed, SetShuttleMaxAngularSpeed, true);
            configManager.OnValueChanged(CCVars.ShuttleIdleLinearDamping, SetShuttleIdleLinearDamping, true);
            configManager.OnValueChanged(CCVars.ShuttleIdleAngularDamping, SetShuttleIdleAngularDamping, true);
            configManager.OnValueChanged(CCVars.ShuttleMaxAngularAcc, SetShuttleMaxAngularAcc, true);
            configManager.OnValueChanged(CCVars.ShuttleMaxAngularMomentum, SetShuttleMaxAngularMomentum, true);
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

        private void SetShuttleMaxLinearSpeed(float value) => ShuttleMaxLinearSpeed = value;
        private void SetShuttleMaxAngularSpeed(float value) => ShuttleMaxAngularSpeed = value;
        private void SetShuttleMaxAngularAcc(float value) => ShuttleMaxAngularAcc = value;
        private void SetShuttleMaxAngularMomentum(float value) => ShuttleMaxAngularMomentum = value;
        private void SetShuttleIdleLinearDamping(float value) => ShuttleIdleLinearDamping = value;
        private void SetShuttleIdleAngularDamping(float value) => ShuttleIdleAngularDamping = value;

        public override void Shutdown()
        {
            base.Shutdown();
            ShutdownEscape();
            ShutdownEmergencyConsole();
            _configManager.UnsubValueChanged(CCVars.ShuttleMaxLinearSpeed, SetShuttleMaxLinearSpeed);
            _configManager.UnsubValueChanged(CCVars.ShuttleMaxAngularSpeed, SetShuttleMaxAngularSpeed);
            _configManager.UnsubValueChanged(CCVars.ShuttleIdleLinearDamping, SetShuttleIdleLinearDamping);
            _configManager.UnsubValueChanged(CCVars.ShuttleIdleAngularDamping, SetShuttleIdleAngularDamping);
            _configManager.UnsubValueChanged(CCVars.ShuttleMaxAngularMomentum, SetShuttleMaxAngularMomentum);
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
                _fixtures.SetMass(fixture, fixture.Area * TileMassMultiplier, manager, false);
                _fixtures.SetRestitution(fixture, 0.1f, manager, false);
            }

            _fixtures.FixtureUpdate(manager, args.NewFixtures[0].Body);
        }

        private void OnGridInit(GridInitializeEvent ev)
        {
            EntityManager.EnsureComponent<ShuttleComponent>(ev.EntityUid);
        }

        private void OnShuttleStartup(EntityUid uid, ShuttleComponent component, ComponentStartup args)
        {
            if (!EntityManager.HasComponent<IMapGridComponent>(component.Owner))
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

        /// <summary>
        /// Enables or disables a shuttle's piloting controls.
        /// </summary>
        public void SetPilotable(ShuttleComponent component, bool value)
        {
            if (component.CanPilot == value) return;
            component.CanPilot = value;

            foreach (var comp in EntityQuery<ShuttleConsoleComponent>(true))
            {
                comp.CanPilot = value;

                // I'm gonna pray if the UI is force closed and we block UI opens that BUI handles it.
                if (!value)
                    _uiSystem.GetUiOrNull(comp.Owner, ShuttleConsoleUiKey.Key)?.CloseAll();
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
            //component.FixedRotation = false; TODO WHEN ROTATING SHUTTLES FIXED.
            component.FixedRotation = false;
            component.LinearDamping = ShuttleIdleLinearDamping;
            component.AngularDamping = ShuttleIdleAngularDamping;
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

            if (!EntityManager.TryGetComponent(component.Owner, out FixturesComponent? fixturesComponent))
            {
                return;
            }

            foreach (var fixture in fixturesComponent.Fixtures.Values)
            {
                fixture.Mass = 0f;
            }
        }
    }
}
