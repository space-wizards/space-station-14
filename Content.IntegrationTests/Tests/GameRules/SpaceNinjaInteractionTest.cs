using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Communications;
using Content.Server.CriminalRecords.Systems;
using Content.Server.GameTicking;
using Content.Server.Objectives.Components;
using Content.Server.Research.Systems;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Ninja.Components;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.GameRules;

public sealed partial class SpaceNinjaInteractionTest : InteractionTest
{
    public abstract class HackTestSystem<TComp, TEvent> : EntitySystem
        where TComp : Component
    {
        public bool Hacked { get; private set; }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TComp, TEvent>(OnHack);
        }

        private void OnHack(Entity<TComp> entity, ref TEvent args)
        {
            Hacked = true;
        }
    }

    [RegisterComponent] public sealed partial class CriminalRecordsHackTestComponent : Component;
    [RegisterComponent] public sealed partial class ResearchServerHackTestComponent : Component;
    [RegisterComponent] public sealed partial class CommsConsoleHackTestComponent : Component;
    public sealed class CriminalRecordsHackTestSystem : HackTestSystem<CriminalRecordsHackTestComponent, CriminalRecordsHackedEvent>;
    public sealed class ResearchServerHackTestSystem : HackTestSystem<ResearchServerHackTestComponent, ResearchStolenEvent>;
    public sealed class CommsConsoleHackTestSystem : HackTestSystem<CommsConsoleHackTestComponent, ThreatCalledInEvent>;


    // ProtoIds of things we need to spawn, check for, etc.
    private static readonly EntProtoId CriminalRecordsConsoleProtoId = "ComputerCriminalRecords";
    private static readonly EntProtoId ResearchServerProtoId = "ResearchAndDevelopmentServer";
    private static readonly EntProtoId CommsConsoleProtoId = "ComputerComms";

    private static readonly EntProtoId NinjaSpawnGameRuleProtoId = "NinjaSpawn";
    private static readonly EntProtoId NinjaGlovesProtoId = "ClothingHandsGlovesSpaceNinja";
    private static readonly EntProtoId NinjaSuitProtoId = "ClothingOuterSuitSpaceNinja";
    private static readonly EntProtoId EnergyKatanaProtoId = "EnergyKatana";
    private static readonly EntProtoId PinpointerProtoId = "PinpointerStation";

    // We need a station for the game rule to start up correctly.
    protected override EntProtoId? StationPrototype => "StandardNanotrasenStation";

    // We need a full human mob, not a test dummy.
    protected override string PlayerPrototype => "MobHuman";

    // Handy references to the ninja's equipment.
    private NetEntity? _suit = null;
    private NetEntity? _gloves = null;
    private NetEntity? _katana = null;
    private NetEntity? _pinpointer = null;

    /// <summary>
    /// Makes the player a ninja, spawns a criminal records console and makes the player
    /// interact with the console with gloves off and then on. Makes sure the player entity
    /// receives the event for a successful hack.
    /// </summary>
    [Test]
    public async Task CriminalRecordsHackingTest()
    {
        var testSys = Server.System<CriminalRecordsHackTestSystem>();
        Assert.That(testSys.Hacked, Is.False, "Criminal records were already hacked?");

        // Ninjitize me, Cap'n
        await MakeNinja(Player);

        // Add our tracker component to the ninja
        await Server.WaitPost(() =>
        {
            SEntMan.AddComponent<CriminalRecordsHackTestComponent>(SEntMan.GetEntity(Player));
        });

        // Drop the jetpack so they have an empty hand to use the console
        await Drop();

        // Spawn the console to be hacked
        await SpawnTarget(CriminalRecordsConsoleProtoId);
        Assert.That(testSys.Hacked, Is.False, "Criminal records were hacked when console spawned.");

        // Interact with the console - gloves are not active, so this should not hack
        await Interact();
        Assert.That(testSys.Hacked, Is.False, "Ninja hacked records without activating gloves.");

        // Activate the gloves
        await Server.WaitPost(() =>
        {
            Assert.That(ItemToggleSys.TryActivate(ToServer(_gloves).Value, user: ToServer(Player)),
                "Failed to activate ninja gloves.");
        });

        // Interact with the console again with active gloves
        await Interact();

        // Make sure the records were hacked
        Assert.That(testSys.Hacked);
    }

    /// <summary>
    /// Makes the player a ninja, spawns an R&D server and makes the player
    /// interact with the server with gloves off and then on. Makes sure the player entity
    /// receives the event for a successful hack.
    /// </summary>
    [Test]
    public async Task ResearchServerHackTest()
    {
        var testSys = Server.System<ResearchServerHackTestSystem>();
        Assert.That(testSys.Hacked, Is.False, "Research server was already hacked?");

        // Ninjitize me, Cap'n
        await MakeNinja(Player);

        // Get the research stealing objective and make sure it has no progress
        var mindSys = Server.System<SharedMindSystem>();
        Assert.That(mindSys.TryGetObjectiveComp<StealResearchConditionComponent>(ToServer(Player), out var stealResearchConditionComp));
        Assert.That(stealResearchConditionComp.DownloadedNodes, Is.Empty, "Player already has stolen research.");

        // Add our tracker component to the ninja
        await Server.WaitPost(() =>
        {
            SEntMan.AddComponent<ResearchServerHackTestComponent>(SEntMan.GetEntity(Player));
        });

        // Drop the jetpack so they have an empty hand to interact with the server
        await Drop();

        // Spawn the R&D server
        var researchServer = await SpawnTarget(ResearchServerProtoId);
        Assert.That(testSys.Hacked, Is.False, "Research server was hacked when it spawned.");

        // Grant all technologies to the server so we have something to steal
        var researchSys = Server.System<ResearchSystem>();
        var technologies = ProtoMan.EnumeratePrototypes<TechnologyPrototype>();
        await Server.WaitPost(() =>
        {
            foreach (var technology in technologies)
            {
                researchSys.AddTechnology(ToServer(researchServer), technology);
            }
        });

        // Interact with the server - gloves are not active, so this should not hack
        await Interact();
        Assert.That(testSys.Hacked, Is.False, "Ninja hacked research server without activating gloves.");
        Assert.That(stealResearchConditionComp.DownloadedNodes, Is.Empty, "DownloadedNodes count increased unexpectedly");

        // Activate the gloves
        await Server.WaitPost(() =>
        {
            Assert.That(ItemToggleSys.TryActivate(ToServer(_gloves).Value, user: ToServer(Player)),
                "Failed to activate ninja gloves.");
        });

        // Interact with the server again with active gloves
        await Interact();

        // Make sure the server was hacked
        Assert.That(testSys.Hacked);

        // Make sure the player gained objective progress
        Assert.That(stealResearchConditionComp.DownloadedNodes, Has.Count.GreaterThan(0), "DownloadedNodes count did not increase.");
    }

    /// <summary>
    /// Makes the player a ninja, spawns a communications console and makes the player
    /// interact with the console with gloves off and then on. Makes sure the player entity
    /// receives the event for a successful hack.
    /// </summary>
    [Test]
    public async Task CommsConsoleHackTest()
    {
        var testSys = Server.System<CommsConsoleHackTestSystem>();
        Assert.That(testSys.Hacked, Is.False, "Comms console was already hacked?");

        // Ninjitize me, Cap'n
        await MakeNinja(Player);

        // Add our tracker component to the ninja
        await Server.WaitPost(() =>
        {
            SEntMan.AddComponent<CommsConsoleHackTestComponent>(SEntMan.GetEntity(Player));
        });

        // Drop the jetpack so they have an empty hand to use the console
        await Drop();

        // Spawn the comms console
        var commsConsole = await SpawnTarget(CommsConsoleProtoId);
        Assert.That(testSys.Hacked, Is.False, "Comms console was hacked when it spawned.");

        // Interact with the console - gloves are not active, so this should not hack
        await Interact();
        Assert.That(testSys.Hacked, Is.False, "Ninja hacked comms console without activating gloves.");

        // Activate the gloves
        await Server.WaitPost(() =>
        {
            Assert.That(ItemToggleSys.TryActivate(ToServer(_gloves).Value, user: ToServer(Player)),
                "Failed to activate ninja gloves.");
        });

        // Interact with the console again with active gloves
        await Interact();

        // Make sure the console was hacked
        Assert.That(testSys.Hacked);
    }

    /// <summary>
    /// Turns the target entity into a ninja antag and makes sure that they got their equipment.
    /// </summary>
    /// <param name="target">Entity to turn into a ninja.</param>
    private async Task MakeNinja(NetEntity? target = null)
    {
        target ??= Player;

        var gameTicker = Server.System<GameTicker>();
        var antagSelection = Server.System<AntagSelectionSystem>();
        var inventory = Server.System<InventorySystem>();
        var mindSys = Server.System<SharedMindSystem>();

        var originalCoordinates = Transform.GetMapCoordinates(ToServer(target.Value));

        await Server.WaitAssertion(() =>
        {
            // Add and start the ninja game rule
            Assert.That(gameTicker.StartGameRule(NinjaSpawnGameRuleProtoId, out var ninjaRuleEntity));
            Assert.That(SEntMan.TryGetComponent<AntagSelectionComponent>(ninjaRuleEntity, out var selectionComp));
            // Try to make the target a ninja
            antagSelection.MakeAntag((ninjaRuleEntity, selectionComp), ServerSession, selectionComp.Definitions[0], ignoreSpawner: true);
            // Make sure it worked
            AssertComp<SpaceNinjaComponent>(true, target);
        });

        // Make sure they have their gear
        Assert.That(inventory.TryGetSlotEntity(ToServer(target.Value), "outerClothing", out var suitUid));
        _suit = SEntMan.GetNetEntity(suitUid);
        AssertPrototype(NinjaSuitProtoId, _suit);

        Assert.That(inventory.TryGetSlotEntity(ToServer(target.Value), "gloves", out var glovesUid));
        _gloves = SEntMan.GetNetEntity(glovesUid);
        AssertPrototype(NinjaGlovesProtoId, _gloves);

        Assert.That(inventory.TryGetSlotEntity(ToServer(target.Value), "belt", out var katanaUid));
        _katana = SEntMan.GetNetEntity(katanaUid);
        AssertPrototype(EnergyKatanaProtoId, _katana);

        Assert.That(inventory.TryGetSlotEntity(ToServer(target.Value), "pocket2", out var pinpointerUid));
        _pinpointer = SEntMan.GetNetEntity(pinpointerUid);
        AssertPrototype(PinpointerProtoId, _pinpointer);

        // Move the ninja back to their original position
        Transform.SetMapCoordinates(ToServer(target.Value), originalCoordinates);
    }
}
