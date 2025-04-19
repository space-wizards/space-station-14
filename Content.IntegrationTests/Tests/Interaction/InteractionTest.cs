#nullable enable
using System.Linq;
using System.Numerics;
using Content.Client.Construction;
using Content.Client.Examine;
using Content.Client.Gameplay;
using Content.IntegrationTests.Pair;
using Content.Server.Body.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Stack;
using Content.Server.Tools;
using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Players;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.UnitTesting;
using Content.Shared.Item.ItemToggle;
using Robust.Client.State;

namespace Content.IntegrationTests.Tests.Interaction;

/// <summary>
/// This is a base class designed to make it easier to test various interactions like construction & DoAfters.
///
/// For construction tests, the interactions are intentionally hard-coded and not pulled automatically from the
/// construction graph, even though this may be a pain to maintain. This is because otherwise these tests could not
/// detect errors in the graph pathfinding (e.g., infinite loops, missing steps, etc).
/// </summary>
[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract partial class InteractionTest
{
    protected virtual string PlayerPrototype => "InteractionTestMob";

    protected TestPair Pair = default!;
    protected TestMapData MapData => Pair.TestMap!;

    protected RobustIntegrationTest.ServerIntegrationInstance Server => Pair.Server;
    protected RobustIntegrationTest.ClientIntegrationInstance Client => Pair.Client;

    protected MapId MapId => MapData.MapId;

    /// <summary>
    /// Target coordinates. Note that this does not necessarily correspond to the position of the <see cref="Target"/>
    /// entity.
    /// </summary>
    protected NetCoordinates TargetCoords;

    /// <summary>
    /// Initial player coordinates. Note that this does not necessarily correspond to the position of the
    /// <see cref="Player"/> entity.
    /// </summary>
    protected NetCoordinates PlayerCoords;

    /// <summary>
    /// The player entity that performs all these interactions. Defaults to an admin-observer with 1 hand.
    /// </summary>
    protected NetEntity Player;
    protected EntityUid SPlayer;
    protected EntityUid CPlayer;

    protected ICommonSession ClientSession = default!;
    protected ICommonSession ServerSession = default!;

    /// <summary>
    /// The current target entity. This is the default entity for various helper functions.
    /// </summary>
    /// <remarks>
    /// Note that this target may be automatically modified by various interactions, in particular construction
    /// interactions often swap out entities, and there are helper methods that attempt to automatically upddate
    /// the target entity. See <see cref="CheckTargetChange"/>
    /// </remarks>
    protected NetEntity? Target;

    protected EntityUid? STarget => ToServer(Target);

    protected EntityUid? CTarget => ToClient(Target);

    /// <summary>
    /// When attempting to start construction, this is the client-side ID of the construction ghost.
    /// </summary>
    protected int ConstructionGhostId;

    // SERVER dependencies
    protected IEntityManager SEntMan = default!;
    protected ITileDefinitionManager TileMan = default!;
    protected IMapManager MapMan = default!;
    protected IPrototypeManager ProtoMan = default!;
    protected IGameTiming STiming = default!;
    protected IComponentFactory Factory = default!;
    protected HandsSystem HandSys = default!;
    protected StackSystem Stack = default!;
    protected SharedInteractionSystem InteractSys = default!;
    protected Content.Server.Construction.ConstructionSystem SConstruction = default!;
    protected SharedDoAfterSystem DoAfterSys = default!;
    protected ToolSystem ToolSys = default!;
    protected ItemToggleSystem ItemToggleSys = default!;
    protected InteractionTestSystem STestSystem = default!;
    protected SharedTransformSystem Transform = default!;
    protected SharedMapSystem MapSystem = default!;
    protected ISawmill SLogger = default!;
    protected SharedUserInterfaceSystem SUiSys = default!;

    // CLIENT dependencies
    protected IEntityManager CEntMan = default!;
    protected IGameTiming CTiming = default!;
    protected IUserInterfaceManager UiMan = default!;
    protected IInputManager InputManager = default!;
    protected Robust.Client.GameObjects.InputSystem InputSystem = default!;
    protected ConstructionSystem CConSys = default!;
    protected ExamineSystem ExamineSys = default!;
    protected InteractionTestSystem CTestSystem = default!;
    protected ISawmill CLogger = default!;
    protected SharedUserInterfaceSystem CUiSys = default!;

    // player components
    protected HandsComponent Hands = default!;
    protected DoAfterComponent DoAfters = default!;

    public float TickPeriod => (float) STiming.TickPeriod.TotalSeconds;

    // Simple mob that has one hand and can perform misc interactions.
    [TestPrototypes]
    private const string TestPrototypes = @"
- type: entity
  id: InteractionTestMob
  components:
  - type: Body
    prototype: Aghost
  - type: DoAfter
  - type: Hands
  - type: ComplexInteraction
  - type: MindContainer
  - type: Stripping
  - type: Puller
  - type: Physics
  - type: Tag
    tags:
    - CanPilot
  - type: UserInterface
";

    [SetUp]
    public virtual async Task Setup()
    {
        Pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, Dirty = true });

        // server dependencies
        SEntMan = Server.ResolveDependency<IEntityManager>();
        TileMan = Server.ResolveDependency<ITileDefinitionManager>();
        MapMan = Server.ResolveDependency<IMapManager>();
        ProtoMan = Server.ResolveDependency<IPrototypeManager>();
        Factory = Server.ResolveDependency<IComponentFactory>();
        STiming = Server.ResolveDependency<IGameTiming>();
        HandSys = SEntMan.System<HandsSystem>();
        InteractSys = SEntMan.System<SharedInteractionSystem>();
        ToolSys = SEntMan.System<ToolSystem>();
        ItemToggleSys = SEntMan.System<ItemToggleSystem>();
        DoAfterSys = SEntMan.System<SharedDoAfterSystem>();
        Transform = SEntMan.System<SharedTransformSystem>();
        MapSystem = SEntMan.System<SharedMapSystem>();
        SConstruction = SEntMan.System<Server.Construction.ConstructionSystem>();
        STestSystem = SEntMan.System<InteractionTestSystem>();
        Stack = SEntMan.System<StackSystem>();
        SLogger = Server.ResolveDependency<ILogManager>().RootSawmill;
        SUiSys = Client.System<SharedUserInterfaceSystem>();

        // client dependencies
        CEntMan = Client.ResolveDependency<IEntityManager>();
        UiMan = Client.ResolveDependency<IUserInterfaceManager>();
        CTiming = Client.ResolveDependency<IGameTiming>();
        InputManager = Client.ResolveDependency<IInputManager>();
        InputSystem = CEntMan.System<Robust.Client.GameObjects.InputSystem>();
        CTestSystem = CEntMan.System<InteractionTestSystem>();
        CConSys = CEntMan.System<ConstructionSystem>();
        ExamineSys = CEntMan.System<ExamineSystem>();
        CLogger = Client.ResolveDependency<ILogManager>().RootSawmill;
        CUiSys = Client.System<SharedUserInterfaceSystem>();

        // Setup map.
        await Pair.CreateTestMap();

        PlayerCoords = SEntMan.GetNetCoordinates(Transform.WithEntityId(MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f)), MapData.MapUid));
        TargetCoords = SEntMan.GetNetCoordinates(Transform.WithEntityId(MapData.GridCoords.Offset(new Vector2(1.5f, 0.5f)), MapData.MapUid));
        await SetTile(Plating, grid: MapData.Grid);

        // Get player data
        var sPlayerMan = Server.ResolveDependency<Robust.Server.Player.IPlayerManager>();
        var cPlayerMan = Client.ResolveDependency<Robust.Client.Player.IPlayerManager>();
        if (Client.Session == null)
            Assert.Fail("No player");
        ClientSession = Client.Session!;
        ServerSession = sPlayerMan.GetSessionById(ClientSession.UserId);

        // Spawn player entity & attach
        EntityUid? old = default;
        await Server.WaitPost(() =>
        {
            // Fuck you mind system I want an hour of my life back
            // Mind system is a time vampire
            SEntMan.System<SharedMindSystem>().WipeMind(ServerSession.ContentData()?.Mind);

            old = cPlayerMan.LocalEntity;
            SPlayer = SEntMan.SpawnEntity(PlayerPrototype, SEntMan.GetCoordinates(PlayerCoords));
            Player = SEntMan.GetNetEntity(SPlayer);
            Server.PlayerMan.SetAttachedEntity(ServerSession, SPlayer);
            Hands = SEntMan.GetComponent<HandsComponent>(SPlayer);
            DoAfters = SEntMan.GetComponent<DoAfterComponent>(SPlayer);
        });

        // Check player got attached.
        await RunTicks(5);
        CPlayer = ToClient(Player);
        Assert.That(cPlayerMan.LocalEntity, Is.EqualTo(CPlayer));

        // Delete old player entity.
        await Server.WaitPost(() =>
        {
            if (old != null)
                SEntMan.DeleteEntity(old.Value);
        });

        // Ensure that the player only has one hand, so that they do not accidentally pick up deconstruction products
        await Server.WaitPost(() =>
        {
            // I lost an hour of my life trying to track down how the hell interaction tests were breaking
            // so greatz to this. Just make your own body prototype!
            var bodySystem = SEntMan.System<BodySystem>();
            var hands = bodySystem.GetBodyChildrenOfType(SEntMan.GetEntity(Player), BodyPartType.Hand).ToArray();

            for (var i = 1; i < hands.Length; i++)
            {
                SEntMan.DeleteEntity(hands[i].Id);
            }
        });

        // Change UI state to in-game.
        var state = Client.ResolveDependency<IStateManager>();
        await Client.WaitPost(() => state.RequestStateChange<GameplayState>());

        // Final player asserts/checks.
        await Pair.ReallyBeIdle(5);
        Assert.Multiple(() =>
        {
            Assert.That(CEntMan.GetNetEntity(cPlayerMan.LocalEntity), Is.EqualTo(Player));
            Assert.That(sPlayerMan.GetSessionById(ClientSession.UserId).AttachedEntity, Is.EqualTo(SEntMan.GetEntity(Player)));
        });
    }

    [TearDown]
    public async Task TearDownInternal()
    {
        await Server.WaitPost(() => MapSystem.DeleteMap(MapId));
        await Pair.CleanReturnAsync();
        await TearDown();
    }

    protected virtual Task TearDown()
    {
        return Task.CompletedTask;
    }
}
