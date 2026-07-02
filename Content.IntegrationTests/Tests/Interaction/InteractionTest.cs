#nullable enable
using System.Numerics;
using Content.Client.Construction;
using Content.Client.Examine;
using Content.Client.Gameplay;
using Content.Client.Interaction;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Hands.Systems;
using Content.Server.Stack;
using Content.Server.Tools;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.Input;
using Robust.Client.State;
using Robust.Client.Timing;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Interaction;

/// <summary>
/// This is a base class designed to make it easier to test various interactions like construction &amp; DoAfters.
/// </summary>
/// <remarks>
/// For construction tests, the interactions are intentionally hard-coded and not pulled automatically from the
/// construction graph, even though this may be a pain to maintain. This is because otherwise these tests could not
/// detect errors in the graph pathfinding (e.g., infinite loops, missing steps, etc).
/// </remarks>
[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract partial class InteractionTest : GameTest
{
    /// <summary>
    /// The prototype that will be spawned for the player entity at <see cref="PlayerCoords"/>.
    /// This is not a full humanoid and only has one hand by default.
    /// </summary>
    protected virtual string PlayerPrototype => "InteractionTestMob";

    /// <summary>
    /// The map path to load for the integration test.
    /// If null an empty map with a single 1x1 plating grid will be generated.
    /// </summary>
    protected virtual ResPath? TestMapPath => null;

    protected TestMapData MapData => TestMap!;

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
    [SidedDependency(Side.Server)] protected ITileDefinitionManager TileMan = default!;
    [SidedDependency(Side.Server)] protected IPrototypeManager ProtoMan => SProtoMan;
    protected IGameTiming STiming => SGameTiming;
    [SidedDependency(Side.Server)] protected IComponentFactory Factory = default!;
    [SidedDependency(Side.Server)] protected HandsSystem HandSys = default!;
    [SidedDependency(Side.Server)] protected StackSystem Stack = default!;
    [SidedDependency(Side.Server)] protected SharedInteractionSystem InteractSys = default!;
    [SidedDependency(Side.Server)] protected Content.Server.Construction.ConstructionSystem SConstruction = default!;
    [SidedDependency(Side.Server)] protected SharedDoAfterSystem DoAfterSys = default!;
    [SidedDependency(Side.Server)] protected ToolSystem ToolSys = default!;
    [SidedDependency(Side.Server)] protected ItemToggleSystem ItemToggleSys = default!;
    [SidedDependency(Side.Server)] protected InteractionTestSystem STestSystem = default!;
    [SidedDependency(Side.Server)] protected SharedTransformSystem Transform = default!;
    [SidedDependency(Side.Server)] protected SharedMapSystem MapSystem = default!;
    [SidedDependency(Side.Server)] protected ILogManager SLogMan = default!;
    protected ISawmill SLogger = default!;
    [SidedDependency(Side.Server)] protected SharedUserInterfaceSystem SUiSys = default!;
    [SidedDependency(Side.Server)] protected SharedCombatModeSystem SCombatMode = default!;
    [SidedDependency(Side.Server)] protected SharedGunSystem SGun = default!;

    // CLIENT dependencies
    protected IClientGameTiming CTiming => CGameTiming;
    [SidedDependency(Side.Client)] protected IUserInterfaceManager UiMan = default!;
    [SidedDependency(Side.Client)] protected IInputManager InputManager = default!;
    [SidedDependency(Side.Client)] protected Robust.Client.GameObjects.InputSystem InputSystem = default!;
    [SidedDependency(Side.Client)] protected ConstructionSystem CConSys = default!;
    [SidedDependency(Side.Client)] protected ExamineSystem ExamineSys = default!;
    [SidedDependency(Side.Client)] protected InteractionTestSystem CTestSystem = default!;
    [SidedDependency(Side.Client)] protected ILogManager CLogMan = default!;
    protected ISawmill CLogger = default!;
    [SidedDependency(Side.Client)] protected SharedUserInterfaceSystem CUiSys = default!;
    [SidedDependency(Side.Client)] protected DragDropSystem CDragDropSys = default!;

    // player components
    protected HandsComponent? Hands;
    protected DoAfterComponent? DoAfters;

    public float TickPeriod => (float)STiming.TickPeriod.TotalSeconds;

    // Simple mob that has one hand and can perform misc interactions.
    [TestPrototypes]
    private const string TestPrototypes = @"
- type: entity
  id: InteractionTestMob
  components:
  - type: DoAfter
  - type: Hands
    hands:
      hand_right: # only one hand, so that they do not accidentally pick up deconstruction products
        location: Right
    sortedHands:
    - hand_right
  - type: ComplexInteraction
  - type: MindContainer
  - type: Stripping
  - type: Puller
  - type: Physics
  - type: GravityAffected
  - type: Tag
    tags:
    - CanPilot
  - type: UserInterface
  - type: CombatMode
";

    protected static PoolSettings Default => new()
    {
        Connected = true,
        Dirty = true
    };

    public override PoolSettings PoolSettings => Default;

    /// <summary>
    /// This is here for backward-compatibility.
    /// You probably should switch to <see cref="DoSetup"/>.
    /// </summary>
    public virtual async Task Setup()
    {
        //
    }

    public override async Task DoSetup()
    {
        await base.DoSetup();

        // Setup map.
        if (TestMapPath == null)
            await Pair.CreateTestMap();
        else
            await Pair.LoadTestMap(TestMapPath.Value);

        PlayerCoords = SEntMan.GetNetCoordinates(Transform.WithEntityId(MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f)), MapData.MapUid));
        TargetCoords = SEntMan.GetNetCoordinates(Transform.WithEntityId(MapData.GridCoords.Offset(new Vector2(1.5f, 0.5f)), MapData.MapUid));
        await SetTile(Plating, grid: MapData.Grid);

        // Get player data
        var sPlayerMan = Server.ResolveDependency<Robust.Server.Player.IPlayerManager>();
        var cPlayerMan = Client.ResolveDependency<Robust.Client.Player.IPlayerManager>();
        if (Client.Session == null)
            Assert.Fail("No player");
        ClientSession = Client.Session!;

        SLogger = SLogMan.RootSawmill;
        CLogger = CLogMan.RootSawmill;

        // Spawn player entity & attach
        NetEntity? old = default;
        await Server.WaitPost(() =>
        {
            // Fuck you mind system I want an hour of my life back
            // Mind system is a time vampire
            SEntMan.System<SharedMindSystem>().WipeMind(ServerSession!.ContentData()?.Mind);

            CEntMan.TryGetNetEntity(cPlayerMan.LocalEntity, out old);
            SPlayer = SEntMan.SpawnEntity(PlayerPrototype, SEntMan.GetCoordinates(PlayerCoords));
            Player = SEntMan.GetNetEntity(SPlayer);
            Server.PlayerMan.SetAttachedEntity(ServerSession, SPlayer);
            Hands = SEntMan.GetComponentOrNull<HandsComponent>(SPlayer);
            DoAfters = SEntMan.GetComponentOrNull<DoAfterComponent>(SPlayer);
        });

        // Check player got attached.
        await RunTicks(5);
        CPlayer = ToClient(Player);
        Assert.That(cPlayerMan.LocalEntity, Is.EqualTo(CPlayer));

        // Delete old player entity.
        await Server.WaitPost(() =>
        {
            if (SEntMan.TryGetEntity(old, out var uid))
                SEntMan.DeleteEntity(uid);
        });

        // Change UI state to in-game.
        var state = Client.ResolveDependency<IStateManager>();
        await Client.WaitPost(() => state.RequestStateChange<GameplayState>());

        // Final player asserts/checks.
        await Pair.ReallyBeIdle(5);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(CEntMan.GetNetEntity(cPlayerMan.LocalEntity), Is.EqualTo(Player));
            Assert.That(sPlayerMan.GetSessionById(ClientSession.UserId).AttachedEntity, Is.EqualTo(SEntMan.GetEntity(Player)));
        }
    }

    public override async Task DoTeardown()
    {
        await Server.WaitPost(() => MapSystem.DeleteMap(MapId));

        await TearDown();

        await base.DoTeardown();
    }

    protected virtual Task TearDown()
    {
        return Task.CompletedTask;
    }
}
