using System.Collections.Generic;
using System.Numerics;
using Content.Client.Gameplay;
using Content.IntegrationTests.Pair;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Repairable;
using Robust.Client.State;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.Power.EntitySystems;

[TestFixture] [NonParallelizable]
public sealed class PowerChargeSystemTests
{
    [TestPrototypes]
    private const string Prototypes = """
        - type: entity
          abstract: true
          parent: BaseMachine
          id: BaseMachinePoweredDummy
          components:
          - type: ApcPowerReceiver
            powerLoad: 1000
          - type: ExtensionCableReceiver
          - type: LightningTarget
            priority: 1

        - type: entity
          id: MachineWithACPowerChargeDummy
          parent: BaseMachinePoweredDummy
          components:
          - type: PowerCharge
            # Needs a valid string or else a unit test fails
            windowTitle: gravity-generator-window-title
            idlePower: 50
            activePower: 2500
          - type: UserInterface
            interfaces:
              enum.PowerChargeUiKey.Key:
                type: PowerChargeBoundUserInterface
          - type: ActivatableUI
            key: enum.PowerChargeUiKey.Key
        """;

    /// <summary>
    /// The prototype that will be spawned for the player entity
    /// This is not a full humanoid and only has one hand by default.
    /// </summary>
    private static string PlayerPrototype => "InteractionTestMob";

    private TestPair _pair;
    private MapId _mapId;
    private TestMapData _map;

    private struct ServerProperties
    {
        public RobustIntegrationTest.ServerIntegrationInstance Instance;
        public IEntityManager EntityManager;
        public SharedTransformSystem Transform;
        public SharedMapSystem MapSystem;
        public SharedUserInterfaceSystem UserInterfaceSystem;
        public PowerChargeSystem Sut;
        public EntityUid Player;
    }

    private struct ClientProperties
    {
        public RobustIntegrationTest.ClientIntegrationInstance Instance;
        public IEntityManager EntityManager;
        public EntityUid Player;
    }

    private ServerProperties _server;
    private ClientProperties _client;

    // Track Created Entities, so we can clean up after the test ends
    private readonly List<EntityUid> _entities = [];

    private NetEntity _player;

    private PoolSettings Default => new() { Connected = true, Dirty = true };
    private PoolSettings Settings => Default;

    // Note: This is a shared environment between tests for speed reasons
    //       If a test is destructive, you need to make a new map
    [OneTimeSetUp]
    public async Task Setup()
    {
        _pair = await PoolManager.GetServerClient(Settings);
        _map = await _pair.CreateTestMap();

        _server = new ServerProperties
        {
            Instance = _pair.Server,
        };

        _server.EntityManager = _server.Instance.ResolveDependency<IEntityManager>();
        _server.MapSystem = _server.EntityManager.System<SharedMapSystem>();
        _server.Transform = _server.EntityManager.System<SharedTransformSystem>();
        _server.UserInterfaceSystem = _server.EntityManager.System<SharedUserInterfaceSystem>();
        _server.Sut = _server.EntityManager.System<PowerChargeSystem>();

        _client = new ClientProperties
        {
            Instance = _pair.Client,
        };

        _client.EntityManager = _client.Instance.ResolveDependency<IEntityManager>();

        _mapId = _map.MapId;

        await CreatePlayerEntity();
    }

    [TearDown]
    public async Task TearDownRemoveEntity()
    {
        await _server.Instance.WaitAssertion(() =>
        {
            _entities.ForEach(entUid =>
            {
                var uiComponent = _server.EntityManager.GetComponent<UserInterfaceComponent>(entUid);
                _server.UserInterfaceSystem.CloseUis((entUid, uiComponent));
                _server.EntityManager.DeleteEntity(entUid);
            });
        });
        _entities.Clear();
    }

    [OneTimeTearDown]
    public async Task Teardown()
    {
        await _server.Instance.WaitPost(() => _server.MapSystem.DeleteMap(_mapId));
        await _pair.CleanReturnAsync();
    }

    #region Helpers

    private struct ServerEntityWithComponents
    {
        public EntityUid EntityId;
        public ApcPowerReceiverComponent ApcPowerReceiver;
        public PowerChargeComponent PowerCharge;

        public void Deconstruct(out EntityUid entityId,
            out ApcPowerReceiverComponent apcPowerReceiverComponent,
            out PowerChargeComponent powerChargeComponent)
        {
            entityId = EntityId;
            apcPowerReceiverComponent = ApcPowerReceiver;
            powerChargeComponent = PowerCharge;
        }
    }

    /// <summary>
    /// Creates and attaches a player into the map
    /// Taken from <see cref="InteractionTest.Setup" />
    /// </summary>
    private async Task CreatePlayerEntity()
    {
        // Get player data
        var sPlayerMan = _server.Instance.ResolveDependency<IPlayerManager>();
        var cPlayerMan = _client.Instance.ResolveDependency<Robust.Client.Player.IPlayerManager>();
        if (_client.Instance.Session == null)
            Assert.Fail("No player");
        var clientSession = _client.Instance.Session;
        var serverSession = sPlayerMan.GetSessionById(clientSession.UserId);

        var playerCoords =
            _server.EntityManager.GetNetCoordinates(
                _server.Transform.WithEntityId(_map.GridCoords.Offset(new Vector2(0.5f, 0.5f)), _map.MapUid));

        // Spawn player entity & attach
        NetEntity? old = null;
        await _server.Instance.WaitAssertion(() =>
        {
            _server.EntityManager.System<SharedMindSystem>().WipeMind(serverSession.ContentData()?.Mind);

            _client.EntityManager.TryGetNetEntity(cPlayerMan.LocalEntity, out old);
            _server.Player = _server.EntityManager.SpawnEntity(PlayerPrototype,
                _server.EntityManager.GetCoordinates(playerCoords));
            _player = _server.EntityManager.GetNetEntity(_server.Player);
            sPlayerMan.SetAttachedEntity(serverSession, _server.Player);
        });

        // Check player got attached.
        await _pair.RunTicksSync(10);
        _client.Player = _client.EntityManager.GetEntity(_player);
        Assert.That(cPlayerMan.LocalEntity, Is.EqualTo(_client.Player));

        // Delete old player entity.
        await _server.Instance.WaitPost(() =>
        {
            if (_server.EntityManager.TryGetEntity(old, out var uid))
                _server.EntityManager.DeleteEntity(uid);
        });

        // Change UI state to in-game.
        var state = _client.Instance.ResolveDependency<IStateManager>();
        await _client.Instance.WaitPost(() => state.RequestStateChange<GameplayState>());

        // Final player asserts/checks.
        await _pair.ReallyBeIdle(5);
        Assert.Multiple(() =>
        {
            Assert.That(_client.EntityManager.GetNetEntity(cPlayerMan.LocalEntity), Is.EqualTo(_player));
            Assert.That(sPlayerMan.GetSessionById(clientSession.UserId).AttachedEntity,
                Is.EqualTo(_server.EntityManager.GetEntity(_player)));
        });
    }

    private async Task<ServerEntityWithComponents> CreatePowerChargeEntity(string entityName)
    {
        PowerChargeComponent powerChargeComponent = null!;
        ApcPowerReceiverComponent apcPowerReceiverComponent = null!;
        EntityUid ent = default!;

        await _server.Instance.WaitAssertion(() =>
        {
            var coords =
                _server.EntityManager.GetNetCoordinates(_server.Transform.WithEntityId(
                    _map.GridCoords.Offset(new Vector2(1.5f, 0.5f)),
                    _map.MapUid));

            ent = _server.EntityManager.SpawnEntity(
                entityName,
                _server.EntityManager.GetCoordinates(coords));

            _entities.Add(ent);

            _server.Instance.RunTicks(1);

            powerChargeComponent = _server.EntityManager.GetComponent<PowerChargeComponent>(ent);
            apcPowerReceiverComponent =
                _server.EntityManager.GetComponent<ApcPowerReceiverComponent>(ent);
        });

        return new ServerEntityWithComponents
        {
            EntityId = ent,
            PowerCharge = powerChargeComponent,
            ApcPowerReceiver = apcPowerReceiverComponent,
        };
    }

    private async Task<Entity<UserInterfaceComponent>> SetupAndOpenUi(
        EntityUid powerChangeEntityUid,
        PowerChargeComponent powerChargeComponent)
    {
        var userInterfaceComponent = _server.EntityManager.GetComponent<UserInterfaceComponent>(powerChangeEntityUid);
        var powerChargeEntity = new Entity<UserInterfaceComponent>(powerChangeEntityUid, userInterfaceComponent);

        await _server.Instance.WaitAssertion(() =>
        {
            _server.UserInterfaceSystem.OpenUi(powerChargeEntity, powerChargeComponent.UiKey, _server.Player);
        });

        // Allow client and server to get in-sync
        await _pair.RunTicksSync(1);

        return powerChargeEntity;
    }

    #endregion

    [Test(Description = "On MapLoad - Doesn't crash")]
    public async Task InitializationTest1()
    {
        // Arrange
        var (_, apcPowerReceiverComponent, powerChargeComponent) =
            await CreatePowerChargeEntity("MachineWithACPowerChargeDummy");

        // Act

        // Assert
        Assert.That(apcPowerReceiverComponent.Load, Is.EqualTo(powerChargeComponent.ActivePowerUse));
    }

    [Test(Description = "On Repair - Sets Intact properly")]
    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task RepairTest1(bool initialState, bool shouldChargeBeZero)
    {
        // Arrange
        var (ent, _, powerChargeComponent) =
            await CreatePowerChargeEntity("MachineWithACPowerChargeDummy");

        powerChargeComponent.Charge = .52f;
        powerChargeComponent.Intact = initialState;
        var expectedCharge = shouldChargeBeZero ? 0f : powerChargeComponent.Charge;

        // Act
        await _server.Instance.WaitAssertion(() =>
        {
            var args = new RepairedEvent
            {
                Ent = ent,
            };
            _server.EntityManager.EventBus.RaiseLocalEvent(ent, ref args);
        });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(powerChargeComponent.Intact, Is.True);
            Assert.That(powerChargeComponent.Charge, Is.EqualTo(expectedCharge));
        });
    }

    [Test(Description = "OnAnchorStateChange - resets status when unanchored")]
    public async Task AnchorChangeTest1()
    {
        // Arrange
        var (ent, _, powerChargeComponent) =
            await CreatePowerChargeEntity("MachineWithACPowerChargeDummy");
        powerChargeComponent.Active = true;
        powerChargeComponent.Charge = .52f;

        // Act
        await _server.Instance.WaitAssertion(() =>
        {
            var args = new AnchorStateChangedEvent(ent, new TransformComponent { Anchored = false });

            _server.EntityManager.EventBus.RaiseLocalEvent(ent, ref args);
        });

        // Assert
        await _server.Instance.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(powerChargeComponent.Charge, Is.EqualTo(0f));
                Assert.That(powerChargeComponent.Active, Is.False);
            });
        });
    }

    [Test(Description = "OnBreak - Intact -> false")]
    public async Task BreakTest1()
    {
        // Arrange
        var (ent, _, powerChargeComponent) =
            await CreatePowerChargeEntity("MachineWithACPowerChargeDummy");
        powerChargeComponent.Intact = true;

        // Act
        await _server.Instance.WaitAssertion(() =>
        {
            var args = new BreakageEventArgs();

            _server.EntityManager.EventBus.RaiseLocalEvent(ent, args);
        });

        // Assert
        await _server.Instance.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(powerChargeComponent.Intact, Is.False);
            });
        });
    }

    [Test(Description = "OnSwitchCharging - Handles switching to the proper mode")]
    [TestCase(true, true, Description = "Remains")]
    [TestCase(false, true, Description = "Toggles")]
    [TestCase(false, false, Description = "Remains")]
    [TestCase(true, false, Description = "Toggles")]
    public async Task SwitchCharging1(bool previousStatus, bool switchToStatus)
    {
        // Arrange
        var (ent, _, powerChargeComponent) =
            await CreatePowerChargeEntity("MachineWithACPowerChargeDummy");

        powerChargeComponent.SwitchedOn = previousStatus;

        // Act
        await _server.Instance.WaitAssertion(() =>
        {
            var args = new SwitchChargingMachineMessage(switchToStatus);

            _server.EntityManager.EventBus.RaiseLocalEvent(ent, args);
        });

        // Assert
        await _server.Instance.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(powerChargeComponent.SwitchedOn, Is.EqualTo(switchToStatus));
            });
        });
    }

    [Test(Description = "UI Updates - Opens and initializes properly")]
    [TestCase(true, true)]
    [TestCase(false, false)]
    public async Task UIPowerChargeStatusTest1(bool switchedOn, bool expectedResult)
    {
        // Arrange
        var (ent, _, powerChargeComponent) =
            await CreatePowerChargeEntity("MachineWithACPowerChargeDummy");
        var powerChargeEntity = await SetupAndOpenUi(ent, powerChargeComponent);

        powerChargeComponent.NeedUIUpdate = true;
        powerChargeComponent.SwitchedOn = switchedOn;

        // Act
        _server.Sut.Update(1);

        // Assert
        var isOpen = _server.UserInterfaceSystem.IsUiOpen(powerChargeEntity, powerChargeComponent.UiKey);
        _server.UserInterfaceSystem.TryGetUiState(powerChargeEntity,
            powerChargeComponent.UiKey,
            out PowerChargeState userInterfaceState);

        await _server.Instance.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(isOpen, Is.True);
                Assert.That(userInterfaceState, Is.Not.Null);
                Assert.That(userInterfaceState.On, Is.EqualTo(expectedResult));
            });
        });
    }

    [Test(Description = "UI Updates - Handles status")]
    [TestCase(true, .01f, .01f, PowerChargePowerStatus.Charging)]
    [TestCase(true, 1f, .01f, PowerChargePowerStatus.FullyCharged)]
    [TestCase(false, 1f, .01f, PowerChargePowerStatus.Discharging)]
    [TestCase(false, 0f, .01f, PowerChargePowerStatus.Off)]
    public async Task UIPowerChargeStatusTest2(bool isOn,
        float charge,
        float chargeRate,
        PowerChargePowerStatus expectedResult)
    {
        // Arrange
        var (ent, apcPowerReceiverComponent, powerChargeComponent) =
            await CreatePowerChargeEntity("MachineWithACPowerChargeDummy");
        var powerChargeEntity = await SetupAndOpenUi(ent, powerChargeComponent);

        powerChargeComponent.NeedUIUpdate = true;
        powerChargeComponent.SwitchedOn = isOn;
        powerChargeComponent.Charge = charge;
        powerChargeComponent.ChargeRate = chargeRate;
        apcPowerReceiverComponent.Powered = true;

        // Act
        _server.Sut.Update(1);

        // Assert
        var isOpen = _server.UserInterfaceSystem.IsUiOpen(powerChargeEntity, powerChargeComponent.UiKey);
        _server.UserInterfaceSystem.TryGetUiState(powerChargeEntity,
            powerChargeComponent.UiKey,
            out PowerChargeState userInterfaceState);

        await _server.Instance.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(isOpen, Is.True);
                Assert.That(userInterfaceState, Is.Not.Null);
                Assert.That(userInterfaceState.PowerStatus, Is.EqualTo(expectedResult));
            });
        });
    }

    [Test(Description = "UI Updates - Closes on Break")]
    public async Task UIPowerChargeStatusTest3()
    {
        // Arrange
        var (ent, _, powerChargeComponent) =
            await CreatePowerChargeEntity("MachineWithACPowerChargeDummy");
        var powerChargeEntity = await SetupAndOpenUi(ent, powerChargeComponent);

        powerChargeComponent.NeedUIUpdate = true;
        powerChargeComponent.SwitchedOn = true;

        // Act
        await _server.Instance.WaitAssertion(() =>
        {
            var args = new BreakageEventArgs();
            _server.EntityManager.EventBus.RaiseLocalEvent(ent, args);
        });
        await _pair.RunTicksSync(1);

        // Assert
        var isOpen = _server.UserInterfaceSystem.IsUiOpen(powerChargeEntity, powerChargeComponent.UiKey);

        await _server.Instance.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(isOpen, Is.False);
            });
        });
    }
}
