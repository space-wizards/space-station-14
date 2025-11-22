#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Cargo.Components;
using Content.Server.Forensics;
using Content.Server.Maps;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Delivery;
using Content.Shared.FingerprintReader;
using Content.Shared.Forensics.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Content.Shared.Shuttles.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Cargo;

public sealed class DeliveryInteractionTest : InteractionTest
{
    private const string LetterDeliveryProtoId = "InteractionTestLetterDelivery";
    private const string LootItemProtoId = "DeliveryInteractionTestItem";
    private const string StationMapId = "DeliveryInteractionStationMap";
    private const string DummyJob = "DeliveryInteractionDummy";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: gameMap
  id: {StationMapId}
  minPlayers: 0
  mapName: DeliveryInteractionStation
  mapPath: /Maps/Test/empty.yml
  stations:
    Station:
      mapNameTemplate: DeliveryInteractionStation
      stationProto: StandardNanotrasenStation
      components: []

- type: playTimeTracker
  id: DeliveryInteractionDummyPlayTime

- type: job
  id: {DummyJob}
  name: {DummyJob}
  playTimeTracker: DeliveryInteractionDummyPlayTime

- type: entity
  parent: BaseItem
  id: {LootItemProtoId}
  name: {LootItemProtoId}

- type: entity
  parent: LetterDelivery
  id: {LetterDeliveryProtoId}
  components:
  - type: EntityTableContainerFill
    containers:
      delivery: !type:AllSelector
        children:
          - id: {LootItemProtoId}
";

    [Test]
    public async Task UnlockAndOpenTest()
    {
        // The test environment doesn't set up a station, so we set it up manually here
        var stationSys = SEntMan.System<StationSystem>();
        var stationMapProto = ProtoMan.Index<GameMapPrototype>(StationMapId);
        EntityUid? station = null;
        await Server.WaitPost(() => station = stationSys.InitializeNewStation(stationMapProto.Stations["Station"], [MapData.Grid]));
        Assert.That(station.HasValue, "Failed to set up station.");
        Assert.That(TryComp<StationRecordsComponent>(SEntMan.GetNetEntity(station), out var records), "Failed to find station records.");
        Assert.That(records, Is.Not.Null);
        var recordsSys = SEntMan.System<StationRecordsSystem>();
        // The player was spawned before we set up the station, so we need to manually add a record for them
        await Server.WaitPost(() =>
        {
            recordsSys.CreateGeneralRecord(station.Value,
                null,
                Identity.Name(ToServer(Player), SEntMan),
                30, // The details don't really matter, we just need a record
                "Urist",
                Robust.Shared.Enums.Gender.Male,
                DummyJob,
                null,
                null,
                new HumanoidCharacterProfile(),
                records
            );
        });

        // Record the station's initial bank balance
        Assert.That(TryComp<StationBankAccountComponent>(SEntMan.GetNetEntity(station), out var bankAccount), "Failed to find station bank balance.");
        Assert.That(bankAccount, Is.Not.Null);
        var initialBankBalance = bankAccount.Balance;

        // Spawn the delivery and make sure it has the needed components
        await SpawnTarget(LetterDeliveryProtoId);
        Assert.That(TryComp<DeliveryComponent>(out var deliveryComp), $"{LetterDeliveryProtoId} does not have DeliveryComponent!");
        Assert.That(deliveryComp, Is.Not.Null);
        Assert.That(TryComp<FingerprintReaderComponent>(out var fingerprintReaderComp), $"{LetterDeliveryProtoId} does not have FingerprintReaderComponent!");
        Assert.That(fingerprintReaderComp, Is.Not.Null);

        // Make sure that the delivery was correctly assigned to the one station record
        Assert.That(deliveryComp.RecipientName, Is.EqualTo(Identity.Name(ToServer(Player), SEntMan)), "Delivery assigned to wrong target.");
        Assert.That(deliveryComp.RecipientJobTitle, Is.EqualTo(DummyJob), "Delivery given wrong job title.");
        Assert.That(deliveryComp.RecipientStation, Is.EqualTo(station), "Delivery assigned to wrong station.");

        // The test player mob does not have fingerprints, so let's give it some
        FingerprintComponent? fingerprintComp = null;
        await Server.WaitPost(() => fingerprintComp = SEntMan.EnsureComponent<FingerprintComponent>(SEntMan.GetEntity(Player)));
        var forensicsSys = Server.System<ForensicsSystem>();
        forensicsSys.RandomizeFingerprint(SEntMan.GetEntity(Player));

        Assert.That(deliveryComp.IsLocked, $"{LetterDeliveryProtoId} spawned unlocked.");
        Assert.That(deliveryComp.IsOpened, Is.False, $"{LetterDeliveryProtoId} spawned opened.");

        // Get the delivery into the player's hand
        await Pickup();

        // Remove the player's fingerprint
        var fingerprintReaderSys = Server.System<FingerprintReaderSystem>();
        var fingerprintReaderEnt = (SEntMan.GetEntity(Target)!.Value, fingerprintReaderComp!);
        // Setting no allowed fingerprints authorizes anybody, so we set a bogus one
        fingerprintReaderSys.SetAllowedFingerprints(fingerprintReaderEnt, ["MonkeysOnTypewriters"]);

        // Try to unlock the delivery without an authorized fingerprint
        await UseInHand();
        Assert.That(deliveryComp.IsLocked, "Delivery unlocked with wrong fingerprint.");
        Assert.That(deliveryComp.IsOpened, Is.False, "Delivery opened prematurely.");

        // Still no reward
        Assert.That(bankAccount.Balance, Is.EqualTo(initialBankBalance), "Reward granted without successful unlock.");

        // Authorize the player's fingerprint
        fingerprintReaderSys.AddAllowedFingerprint(fingerprintReaderEnt, fingerprintComp!.Fingerprint!);

        // Unlock the delivery with the correct fingerprint
        await UseInHand();
        Assert.That(deliveryComp.IsLocked, Is.False, "Failed to unlock delivery with fingerprint.");
        Assert.That(deliveryComp.IsOpened, Is.False, "Delivery opened prematurely.");

        // Make sure the station's bank balance increased by unlocking the delivery
        Assert.That(bankAccount.Balance, Is.GreaterThan(initialBankBalance), "Station was not granted reward for unlocking delivery.");
        var balanceAfterUnlock = bankAccount.Balance;

        // Open the unlocked delivery
        await UseInHand();

        // Make sure the station's bank balance was not affected by opening the delivery
        Assert.That(deliveryComp.IsOpened, "Failed to open.");
        Assert.That(bankAccount.Balance, Is.EqualTo(balanceAfterUnlock), "Station bank balance changed after opening delivery.");

        // Make sure the player is now holding the loot item
        Assert.That(Hands.ActiveHandEntity, Is.Not.Null, "Player should be holding loot item, but isn't holding anything.");
        Assert.That(TryComp<MetaDataComponent>(SEntMan.GetNetEntity(Hands.ActiveHandEntity.Value), out var heldMetaComp), "Failed to find MetaDataComponent on held item?");
        Assert.That(heldMetaComp, Is.Not.Null);
        Assert.That(heldMetaComp.EntityPrototype, Is.Not.Null, "Loot item prototype unknown.");
        Assert.That(heldMetaComp.EntityPrototype.ID, Is.EqualTo(LootItemProtoId), "Loot item was not expected prototype.");

        // Make sure the delivery still exists
        AssertExists();

        // Make sure the delivery is now considered space garbage
        AssertComp<SpaceGarbageComponent>(true);
    }
}
