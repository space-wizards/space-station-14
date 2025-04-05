using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.CriminalRecords.Systems;
using Content.Server.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Ninja.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.GameRules;

public sealed partial class SpaceNinjaInteractionTest : InteractionTest
{
    public sealed class CriminalRecordsHackTestSystem : EntitySystem
    {
        public bool Hacked { get; private set; }

        public override void Initialize()
        {
            SubscribeLocalEvent<CriminalRecordsHackTestComponent, CriminalRecordsHackedEvent>(OnHack);
        }

        private void OnHack(Entity<CriminalRecordsHackTestComponent> entity, ref CriminalRecordsHackedEvent args)
        {
            Hacked = true;
        }
    }

    // This exists just so the test system has a way of subscribing to the event on the ninja entity.
    // We can't SpaceNinjaComponent because it's already subscribed.
    [RegisterComponent]
    public sealed partial class CriminalRecordsHackTestComponent : Component;

    // ProtoIds of things we need to spawn, check for, etc.
    private static readonly EntProtoId CriminalRecordsConsoleProtoId = "ComputerCriminalRecords";
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
            antagSelection.MakeAntag((ninjaRuleEntity, selectionComp), ClientSession, selectionComp.Definitions[0], ignoreSpawner: true);
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
