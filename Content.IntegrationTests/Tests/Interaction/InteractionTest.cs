#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Content.Client.Construction;
using Content.Client.Examine;
using Content.Server.Body.Systems;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Server.Stack;
using Content.Server.Tools;
using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.UnitTesting;

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
    protected virtual string PlayerPrototype => "AdminObserver";

    protected PairTracker PairTracker = default!;
    protected TestMapData MapData = default!;

    protected RobustIntegrationTest.ServerIntegrationInstance Server => PairTracker.Pair.Server;
    protected RobustIntegrationTest.ClientIntegrationInstance Client => PairTracker.Pair.Client;

    protected MapId MapId => MapData.MapId;

    /// <summary>
    /// Target coordinates. Note that this does not necessarily correspond to the position of the <see cref="Target"/>
    /// entity.
    /// </summary>
    protected EntityCoordinates TargetCoords;

    /// <summary>
    /// Initial player coordinates. Note that this does not necessarily correspond to the position of the
    /// <see cref="Player"/> entity.
    /// </summary>
    protected EntityCoordinates PlayerCoords;

    /// <summary>
    /// The player entity that performs all these interactions. Defaults to an admin-observer with 1 hand.
    /// </summary>
    protected EntityUid Player;

    protected ICommonSession ClientSession = default!;
    protected IPlayerSession ServerSession = default!;

    /// <summary>
    /// The current target entity. This is the default entity for various helper functions.
    /// </summary>
    /// <remarks>
    /// Note that this target may be automatically modified by various interactions, in particular construction
    /// interactions often swap out entities, and there are helper methods that attempt to automatically upddate
    /// the target entity. See <see cref="CheckTargetChange"/>
    /// </remarks>
    protected EntityUid? Target;

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
    protected SharedHandsSystem HandSys = default!;
    protected StackSystem Stack = default!;
    protected SharedInteractionSystem InteractSys = default!;
    protected Content.Server.Construction.ConstructionSystem SConstruction = default!;
    protected SharedDoAfterSystem DoAfterSys = default!;
    protected ToolSystem ToolSys = default!;
    protected InteractionTestSystem STestSystem = default!;
    protected SharedTransformSystem Transform = default!;

    // CLIENT dependencies
    protected IEntityManager CEntMan = default!;
    protected IGameTiming CTiming = default!;
    protected IUserInterfaceManager UiMan = default!;
    protected IInputManager InputManager = default!;
    protected InputSystem InputSystem = default!;
    protected ConstructionSystem CConSys = default!;
    protected ExamineSystem ExamineSys = default!;
    protected InteractionTestSystem CTestSystem = default!;

    // player components
    protected HandsComponent Hands = default!;
    protected DoAfterComponent DoAfters = default!;

    public float TickPeriod => (float)STiming.TickPeriod.TotalSeconds;

    [SetUp]
    public virtual async Task Setup()
    {
        PairTracker = await PoolManager.GetServerClient(new PoolSettings());

        // server dependencies
        SEntMan = Server.ResolveDependency<IEntityManager>();
        TileMan = Server.ResolveDependency<ITileDefinitionManager>();
        MapMan = Server.ResolveDependency<IMapManager>();
        ProtoMan = Server.ResolveDependency<IPrototypeManager>();
        Factory = Server.ResolveDependency<IComponentFactory>();
        STiming = Server.ResolveDependency<IGameTiming>();
        HandSys = SEntMan.System<SharedHandsSystem>();
        InteractSys = SEntMan.System<SharedInteractionSystem>();
        ToolSys = SEntMan.System<ToolSystem>();
        DoAfterSys = SEntMan.System<SharedDoAfterSystem>();
        Transform = SEntMan.System<SharedTransformSystem>();
        SConstruction = SEntMan.System<Content.Server.Construction.ConstructionSystem>();
        STestSystem = SEntMan.System<InteractionTestSystem>();
        Stack = SEntMan.System<StackSystem>();

        // client dependencies
        CEntMan = Client.ResolveDependency<IEntityManager>();
        UiMan = Client.ResolveDependency<IUserInterfaceManager>();
        CTiming = Client.ResolveDependency<IGameTiming>();
        InputManager = Client.ResolveDependency<IInputManager>();
        InputSystem = CEntMan.System<InputSystem>();
        CTestSystem = CEntMan.System<InteractionTestSystem>();
        CConSys = CEntMan.System<ConstructionSystem>();
        ExamineSys = CEntMan.System<ExamineSystem>();

        // Setup map.
        MapData = await PoolManager.CreateTestMap(PairTracker);
        PlayerCoords = MapData.GridCoords.Offset((0.5f, 0.5f)).WithEntityId(MapData.MapUid, Transform, SEntMan);
        TargetCoords = MapData.GridCoords.Offset((1.5f, 0.5f)).WithEntityId(MapData.MapUid, Transform, SEntMan);
        await SetTile(Plating, grid: MapData.MapGrid);

        // Get player data
        var sPlayerMan = Server.ResolveDependency<Robust.Server.Player.IPlayerManager>();
        var cPlayerMan = Client.ResolveDependency<Robust.Client.Player.IPlayerManager>();
        if (cPlayerMan.LocalPlayer?.Session == null)
            Assert.Fail("No player");
        ClientSession = cPlayerMan.LocalPlayer!.Session!;
        ServerSession = sPlayerMan.GetSessionByUserId(ClientSession.UserId);

        // Spawn player entity & attach
        EntityUid? old = default;
        await Server.WaitPost(() =>
        {
            // Fuck you mind system I want an hour of my life back
            // Mind system is a time vampire
            ServerSession.ContentData()?.WipeMind();

            old = cPlayerMan.LocalPlayer.ControlledEntity;
            Player = SEntMan.SpawnEntity(PlayerPrototype, PlayerCoords);
            ServerSession.AttachToEntity(Player);
            Hands = SEntMan.GetComponent<HandsComponent>(Player);
            DoAfters = SEntMan.GetComponent<DoAfterComponent>(Player);
        });

        // Check player got attached.
        await RunTicks(5);
        Assert.That(cPlayerMan.LocalPlayer.ControlledEntity, Is.EqualTo(Player));

        // Delete old player entity.
        await Server.WaitPost(() =>
        {
            if (old != null)
                SEntMan.DeleteEntity(old.Value);
        });

        // Ensure that the player only has one hand, so that they do not accidentally pick up deconstruction products
        await Server.WaitPost(() =>
        {
            var bodySystem = SEntMan.System<BodySystem>();
            var hands = bodySystem.GetBodyChildrenOfType(Player, BodyPartType.Hand).ToArray();

            for (var i = 1; i < hands.Length; i++)
            {
                bodySystem.DropPart(hands[i].Id);
                SEntMan.DeleteEntity(hands[i].Id);
            }
        });

        // Final player asserts/checks.
        await PoolManager.ReallyBeIdle(PairTracker.Pair, 5);
        Assert.That(cPlayerMan.LocalPlayer.ControlledEntity, Is.EqualTo(Player));
        Assert.That(sPlayerMan.GetSessionByUserId(ClientSession.UserId).AttachedEntity, Is.EqualTo(Player));
    }

    [TearDown]
    public virtual async Task Cleanup()
    {
        await Server.WaitPost(() => MapMan.DeleteMap(MapId));
        await PairTracker.CleanReturnAsync();
    }
}

