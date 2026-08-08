// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.DeadSpace.PersonnelRecords.Systems;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.DeadSpace.PersonnelRecords;
using Content.Shared.DeadSpace.PersonnelRecords.Components;
using Content.Shared.GameTicking;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.StationRecords;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.DeadSpace.PersonnelRecords;

/// <summary>
/// Regression coverage for two server-side pipelines that are otherwise only exercised through a
/// live multiplayer client: the HUD icon set on Demotion/Dismissal, and the
/// <c>PersonnelRecordsConsoleComponent</c> default department/Security channel wiring.
///
/// <see cref="IconSetOnDismissal"/> needs a tick between renaming the test dummy and checking its
/// resolved identity name - <c>IdentitySystem</c> only renames the nested "identity" entity on its
/// next <c>Update()</c> (queued off <c>EntityRenamedEvent</c>), not synchronously. Skipping that
/// tick is exactly what made an earlier version of this test report a false failure.
/// </summary>
[TestFixture]
public sealed class PersonnelRecordDiagnosticTest
{
    private static readonly ProtoId<JobPrototype> HeadOfPersonnel = "HeadOfPersonnel";
    private static readonly ProtoId<JobPrototype> SecurityOfficer = "SecurityOfficer";

    private static string _map = "PersonnelRecordTestMap";

    [TestPrototypes]
    private static readonly string PersonnelRecordTestMap = @$"
- type: gameMap
  id: {_map}
  mapName: {_map}
  mapPath: /Maps/Test/empty.yml
  minPlayers: 0
  stations:
    Empty:
      stationProto: StandardNanotrasenStation
      components:
        - type: StationNameSetup
          mapNameTemplate: ""Empty""
        - type: StationJobs
          availableJobs:
            Passenger: [ -1, -1 ]
            {HeadOfPersonnel}: [ 1, 1 ]
            {SecurityOfficer}: [ 1, 1 ]
";

    /// <summary>
    /// Full-pipeline test, no shortcuts: spawns a REAL round-started player (real
    /// <c>PlayerSpawnCompleteEvent</c> -&gt; <c>GeneralStationRecord</c> creation, real ID card, real
    /// <c>IdentityComponent</c>) and drives <see cref="PersonnelRecordsSystem.TryIssueOrder"/>
    /// directly - the exact same call <c>PersonnelRecordsConsoleSystem</c> makes when someone issues
    /// a dismissal through the console. If this passes, the automatic assignment pipeline (name
    /// lookup included) is proven correct end to end and the remaining bug has to be client-side or
    /// specific to whatever the live target actually is; if it fails, this pinpoints exactly where
    /// the chain breaks for a real player character instead of a bare test dummy.
    /// </summary>
    [Test]
    public async Task IssueOrderAttachesIconOnRealPlayer()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });

        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();

        server.CfgMan.SetCVar(CCVars.GameMap, _map);
        var ticker = server.System<GameTicker>();

        ticker.ToggleReadyAll(true);
        await server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        var mob = server.PlayerMan.SessionsDict[pair.Client.User!.Value].AttachedEntity;
        Assert.That(mob, Is.Not.Null, "Player never got an attached entity after round start");

        StationRecordKey? key = null;

        await server.WaitAssertion(() =>
        {
            var idCardSystem = entMan.System<SharedIdCardSystem>();
            Assert.That(idCardSystem.TryFindIdCard(mob!.Value, out var idCard), Is.True, "Spawned player has no ID card");
            Assert.That(entMan.TryGetComponent<StationRecordKeyStorageComponent>(idCard, out var keyStorage), Is.True, "ID card has no StationRecordKeyStorageComponent");
            Assert.That(keyStorage!.Key, Is.Not.Null, "ID card's station record key was never set");
            key = keyStorage.Key;

            var records = entMan.System<StationRecordsSystem>();
            Assert.That(records.TryGetRecord<GeneralStationRecord>(key!.Value, out var general), Is.True, "No GeneralStationRecord for the spawned player");
            TestContext.Out.WriteLine($"[DIAG] GeneralStationRecord.Name = '{general.Name}', Identity.Name(mob) = '{Identity.Name(mob!.Value, entMan)}'");
        });

        await server.WaitAssertion(() =>
        {
            var personnelRecords = entMan.System<PersonnelRecordsSystem>();
            var records = entMan.System<StationRecordsSystem>();
            records.TryGetRecord<GeneralStationRecord>(key!.Value, out var general);

            Assert.That(personnelRecords.TryIssueOrder(key!.Value, EmploymentStatus.Dismissal, "integration test", "Test Officer", general.JobPrototype), Is.True, "TryIssueOrder itself refused - record wasn't in a state that allows a fresh dismissal");
        });

        // SyncIdentityIcon runs synchronously inside TryIssueOrder, but give networking/PVS a tick
        // regardless in case anything downstream is queued.
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.HasComponent<PersonnelRecordComponent>(mob!.Value), Is.True, "PersonnelRecordComponent was never attached to the real player entity via TryIssueOrder - the name-match lookup in UpdatePersonnelIdentity did not find them");

            var comp = entMan.GetComponent<PersonnelRecordComponent>(mob!.Value);
            Assert.That(comp.StatusIcon.Id, Is.EqualTo("PersonnelIconDismissal"));
        });

        await server.WaitPost(() => ticker.RestartRound());
        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Same real-player setup as <see cref="IssueOrderAttachesIconOnRealPlayer"/>, but the target's
    /// nested identity entity is renamed away from their real name first - simulating anything that
    /// obscures identity (mask, hood, balaclava, etc.).
    ///
    /// Before the <c>TryGetRecordCharacter</c> fix in <c>PersonnelRecordsSystem.SyncIdentityIcon</c>,
    /// this reproduced the reported bug exactly: the old name-match (<c>Identity.Name</c> with no
    /// viewer always resolves to the disguised name, never the real one) came up empty, so the icon
    /// never got attached even though the order itself went through fine - while a manual VV add
    /// (which does no name lookup at all) worked. Now that the target is resolved directly through
    /// the ID card's <see cref="StationRecordKeyStorageComponent"/> instead, disguise is irrelevant
    /// and this must pass the same way <see cref="IssueOrderAttachesIconOnRealPlayer"/> does.
    /// </summary>
    [Test]
    public async Task IssueOrderFindsDisguisedIdentity()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });

        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();

        server.CfgMan.SetCVar(CCVars.GameMap, _map);
        var ticker = server.System<GameTicker>();

        ticker.ToggleReadyAll(true);
        await server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        var mob = server.PlayerMan.SessionsDict[pair.Client.User!.Value].AttachedEntity;
        Assert.That(mob, Is.Not.Null, "Player never got an attached entity after round start");

        StationRecordKey? key = null;

        await server.WaitAssertion(() =>
        {
            var idCardSystem = entMan.System<SharedIdCardSystem>();
            idCardSystem.TryFindIdCard(mob!.Value, out var idCard);
            entMan.TryGetComponent<StationRecordKeyStorageComponent>(idCard, out var keyStorage);
            key = keyStorage!.Key;

            // Simulate a disguise: rename the nested identity entity to something else entirely,
            // exactly what a mask/hood covering the face would cause.
            Assert.That(entMan.TryGetComponent<IdentityComponent>(mob!.Value, out var identity), Is.True, "Real player has no IdentityComponent");
            var identityEntity = identity!.IdentityEntitySlot?.ContainedEntity;
            Assert.That(identityEntity, Is.Not.Null, "IdentityComponent has no contained identity entity");

            entMan.System<MetaDataSystem>().SetEntityName(identityEntity!.Value, "Someone Unknown");
        });

        await pair.RunTicksSync(2);

        await server.WaitAssertion(() =>
        {
            var records = entMan.System<StationRecordsSystem>();
            records.TryGetRecord<GeneralStationRecord>(key!.Value, out var general);
            TestContext.Out.WriteLine($"[DIAG] GeneralStationRecord.Name = '{general.Name}', Identity.Name(mob) after disguise = '{Identity.Name(mob!.Value, entMan)}'");

            var personnelRecords = entMan.System<PersonnelRecordsSystem>();
            Assert.That(personnelRecords.TryIssueOrder(key!.Value, EmploymentStatus.Dismissal, "integration test", "Test Officer", general.JobPrototype), Is.True);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var hasIcon = entMan.HasComponent<PersonnelRecordComponent>(mob!.Value);
            TestContext.Out.WriteLine($"[DIAG] PersonnelRecordComponent present after disguised-identity order = {hasIcon}");
            Assert.That(hasIcon, Is.True, "PersonnelRecordComponent was not attached despite the direct ID-card-based resolution - disguise should no longer matter");

            var comp = entMan.GetComponent<PersonnelRecordComponent>(mob!.Value);
            Assert.That(comp.StatusIcon.Id, Is.EqualTo("PersonnelIconDismissal"));
        });

        await server.WaitPost(() => ticker.RestartRound());
        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Reproduces the reported bug exactly: dismiss the Head of Personnel (freeing their capped
    /// [1,1] slot so a new hire can fill the vacancy), then reassign the *same* person straight back
    /// to Head of Personnel the way the ID card console does it. The generic record synchronization
    /// must not consume the vacancy by itself; only the explicit <see cref="IdCardJobAssignedEvent"/>
    /// raised after a successful write may reclaim it.
    /// </summary>
    [Test]
    public async Task ReassigningBackDoesNotDoubleFreeSlot()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });

        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();

        server.CfgMan.SetCVar(CCVars.GameMap, _map);
        var ticker = server.System<GameTicker>();

        await pair.SetJobPriorities((HeadOfPersonnel, JobPriority.High));
        ticker.ToggleReadyAll(true);
        await server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        var mob = server.PlayerMan.SessionsDict[pair.Client.User!.Value].AttachedEntity;
        Assert.That(mob, Is.Not.Null, "Player never got an attached entity after round start");

        StationRecordKey? key = null;
        var idCard = default(EntityUid);
        var station = default(EntityUid);
        var originalJobTitle = string.Empty;

        await server.WaitAssertion(() =>
        {
            var idCardSystem = entMan.System<SharedIdCardSystem>();
            Assert.That(idCardSystem.TryFindIdCard(mob!.Value, out var idCardEnt), Is.True);
            idCard = idCardEnt;
            Assert.That(entMan.TryGetComponent<StationRecordKeyStorageComponent>(idCard, out var keyStorage), Is.True);
            key = keyStorage!.Key;
            station = key!.Value.OriginStation;

            var records = entMan.System<StationRecordsSystem>();
            Assert.That(records.TryGetRecord<GeneralStationRecord>(key!.Value, out var general), Is.True);
            Assert.That(general.JobPrototype, Is.EqualTo(HeadOfPersonnel.Id), "Test setup didn't actually get the player hired as Head of Personnel");
            originalJobTitle = general.JobTitle;

            var stationJobs = entMan.System<StationJobsSystem>();
            Assert.That(stationJobs.TryGetJobSlot(station, HeadOfPersonnel.Id, out var freeAtStart), Is.True);
            Assert.That(freeAtStart, Is.EqualTo(0), "The single round-start HoP slot should already be filled (0 free) before any of this");
        });

        // Issue the dismissal, then simulate what the dismiss button/ID card console actually does to
        // execute it: rewrite the job directly. This is what PersonnelOrderCompletionSystem detects
        // and what triggers PersonnelVacancySystem to free the vacated slot.
        await server.WaitAssertion(() =>
        {
            var records = entMan.System<StationRecordsSystem>();
            records.TryGetRecord<GeneralStationRecord>(key!.Value, out var general);

            var personnelRecords = entMan.System<PersonnelRecordsSystem>();
            Assert.That(personnelRecords.TryIssueOrder(key!.Value, EmploymentStatus.Dismissal, "test", "Test Officer", general.JobPrototype), Is.True);

            general.JobPrototype = "Passenger";
            general.JobTitle = "Passenger";
            records.Synchronize(key!.Value);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var stationJobs = entMan.System<StationJobsSystem>();
            Assert.That(stationJobs.TryGetJobSlot(station, HeadOfPersonnel.Id, out var freeAfterDismissal), Is.True);
            Assert.That(freeAfterDismissal, Is.EqualTo(1), "Executing the dismissal should have freed exactly one HoP slot for a replacement hire");

            var records = entMan.System<StationRecordsSystem>();
            Assert.That(records.TryGetRecord<PersonnelRecord>(key!.Value, out var personnel), Is.True);
            Assert.That(personnel.Status, Is.EqualTo(EmploymentStatus.None));
            Assert.That(personnel.PreviousJobTitle, Is.EqualTo(originalJobTitle), "Execution must retain the title from when the order was issued, not the new title");
            Assert.That(personnel.Reason, Is.Null, "An executed order must not leave an active reason on a clean record");
            Assert.That(personnel.InitiatorName, Is.Null, "An executed order must not leave an active initiator on a clean record");
        });

        // Now simulate the Captain reassigning the same person straight back to Head of Personnel via
        // the ID card console - same JobPrototype rewrite + Synchronize, slot pool untouched.
        await server.WaitAssertion(() =>
        {
            var records = entMan.System<StationRecordsSystem>();
            records.TryGetRecord<GeneralStationRecord>(key!.Value, out var general);
            general.JobPrototype = HeadOfPersonnel.Id;
            general.JobTitle = originalJobTitle;
            records.Synchronize(key!.Value);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var stationJobs = entMan.System<StationJobsSystem>();
            Assert.That(stationJobs.TryGetJobSlot(station, HeadOfPersonnel.Id, out var freeBeforeConfirmation), Is.True);
            Assert.That(freeBeforeConfirmation, Is.EqualTo(1), "A generic record update must not consume a real vacancy");

            entMan.EventBus.RaiseEvent(EventSource.Local,
                new IdCardJobAssignedEvent(mob!.Value, idCard, HeadOfPersonnel));

            Assert.That(stationJobs.TryGetJobSlot(station, HeadOfPersonnel.Id, out var freeAfterConfirmation), Is.True);
            Assert.That(freeAfterConfirmation, Is.EqualTo(0), "A confirmed ID-console assignment should reclaim the extra slot");
        });

        await server.WaitPost(() => ticker.RestartRound());
        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// The gap <see cref="ReassigningBackDoesNotDoubleFreeSlot"/> doesn't cover: a *different* hire
    /// already took the freed slot before anyone tries to reassign the original person back. There's
    /// nothing left in the free pool to reclaim at that point, so this has to be stopped before it
    /// happens rather than cleaned up after - <c>PersonnelVacancySystem.OnJobAssignmentAttempt</c>
    /// vetoes the <see cref="IdCardJobAssignmentAttemptEvent"/> IdCardConsoleSystem raises right
    /// before it would otherwise let the reassignment through.
    /// </summary>
    [Test]
    public async Task JobAssignmentBlockedAfterSlotAlreadyRefilled()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });

        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();

        server.CfgMan.SetCVar(CCVars.GameMap, _map);
        var ticker = server.System<GameTicker>();

        await pair.SetJobPriorities((SecurityOfficer, JobPriority.High));
        ticker.ToggleReadyAll(true);
        await server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        var mob = server.PlayerMan.SessionsDict[pair.Client.User!.Value].AttachedEntity;
        Assert.That(mob, Is.Not.Null, "Player never got an attached entity after round start");

        StationRecordKey? key = null;
        var idCard = default(EntityUid);
        var station = default(EntityUid);

        await server.WaitAssertion(() =>
        {
            var idCardSystem = entMan.System<SharedIdCardSystem>();
            Assert.That(idCardSystem.TryFindIdCard(mob!.Value, out var idCardEnt), Is.True);
            idCard = idCardEnt;

            Assert.That(entMan.TryGetComponent<StationRecordKeyStorageComponent>(idCard, out var keyStorage), Is.True);
            key = keyStorage!.Key;
            station = key!.Value.OriginStation;

            var records = entMan.System<StationRecordsSystem>();
            Assert.That(records.TryGetRecord<GeneralStationRecord>(key!.Value, out var general), Is.True);
            Assert.That(general.JobPrototype, Is.EqualTo(SecurityOfficer.Id), "Test setup didn't actually get the player hired as Security Officer");
        });

        // Dismiss, execute, then simulate a *different* new hire consuming the freed slot the normal
        // way - directly adjusting the slot pool is the same net effect the normal spawn/job
        // assignment machinery has, without needing a second real connected client.
        await server.WaitAssertion(() =>
        {
            var records = entMan.System<StationRecordsSystem>();
            records.TryGetRecord<GeneralStationRecord>(key!.Value, out var general);

            var personnelRecords = entMan.System<PersonnelRecordsSystem>();
            Assert.That(personnelRecords.TryIssueOrder(key!.Value, EmploymentStatus.Dismissal, "test", "Test Officer", general.JobPrototype), Is.True);

            general.JobPrototype = "Passenger";
            records.Synchronize(key!.Value);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var stationJobs = entMan.System<StationJobsSystem>();
            Assert.That(stationJobs.TryGetJobSlot(station, SecurityOfficer.Id, out var freeAfterDismissal), Is.True);
            Assert.That(freeAfterDismissal, Is.EqualTo(1), "Executing the dismissal should have freed exactly one Officer slot");

            Assert.That(stationJobs.TryAdjustJobSlot(station, SecurityOfficer.Id, -1, createSlot: false, clamp: true), Is.True);
            Assert.That(stationJobs.TryGetJobSlot(station, SecurityOfficer.Id, out var freeAfterNewHire), Is.True);
            Assert.That(freeAfterNewHire, Is.EqualTo(0), "Simulated new hire should have consumed the freed slot");
        });

        // The dismissed person now tries to get reassigned straight back to Officer - should be
        // blocked, since the slot they'd be taking is already occupied by the new hire.
        await server.WaitAssertion(() =>
        {
            var attempt = new IdCardJobAssignmentAttemptEvent(mob!.Value, idCard, SecurityOfficer);
            entMan.EventBus.RaiseEvent(EventSource.Local, attempt);
            Assert.That(attempt.Cancelled, Is.True, "Reassignment should have been vetoed - the freed slot was already consumed by someone else");
        });

        // Same attempt, but the actor has Central Command access this time - should go through
        // untouched, same precedent as the Captain/IAA/BlueShieldOfficer protected-jobs bypass.
        await server.WaitAssertion(() =>
        {
            var access = entMan.EnsureComponent<AccessComponent>(mob!.Value);
            access.Tags.Add("CentralCommand");

            var attempt = new IdCardJobAssignmentAttemptEvent(mob!.Value, idCard, SecurityOfficer);
            entMan.EventBus.RaiseEvent(EventSource.Local, attempt);
            Assert.That(attempt.Cancelled, Is.False, "Central Command access should bypass the block entirely");
        });

        await server.WaitPost(() => ticker.RestartRound());
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task IconSetOnDismissal()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();

        EntityUid dummy = default;

        await server.WaitAssertion(() =>
        {
            var metaSystem = entMan.System<MetaDataSystem>();
            dummy = entMan.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            metaSystem.SetEntityName(dummy, "PersonnelIconTestDummy");
        });

        // IdentitySystem only renames the nested "identity" entity on its next Update() tick
        // (QueueIdentityUpdate off EntityRenamedEvent), not synchronously on SetEntityName - give it some.
        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            var personnelRecords = entMan.System<PersonnelRecordsSystem>();

            var actualName = Identity.Name(dummy, entMan);
            TestContext.Out.WriteLine($"[DIAG] Identity.Name(dummy) after ticking = '{actualName}'");

            personnelRecords.UpdatePersonnelIdentity("PersonnelIconTestDummy", EmploymentStatus.Dismissal);

            Assert.That(entMan.HasComponent<PersonnelRecordComponent>(dummy), Is.True, "PersonnelRecordComponent was never added to the matching entity");

            var comp = entMan.GetComponent<PersonnelRecordComponent>(dummy);
            Assert.That(comp.StatusIcon.Id, Is.EqualTo("PersonnelIconDismissal"));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ConsoleComponentDefaults()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            var console = entMan.SpawnEntity("ComputerPersonnelRecords", MapCoordinates.Nullspace);
            var comp = entMan.GetComponent<PersonnelRecordsConsoleComponent>(console);

            Assert.That(comp.SecurityChannel.Id, Is.EqualTo("Security"));
            Assert.That(comp.DepartmentChannels["Civilian"].Id, Is.EqualTo("Service"));
            Assert.That(comp.DepartmentChannels["Cargo"].Id, Is.EqualTo("Supply"));
            Assert.That(comp.DepartmentChannels["Engineering"].Id, Is.EqualTo("Engineering"));

            // Captain/IAA/BlueShieldOfficer are visible (not excluded) but only CentCom access can
            // act on them - see PersonnelRecordsConsoleSystem.IsProtectedTarget.
            Assert.That(comp.ExcludedJobs.Select(x => x.Id), Does.Not.Contain("Captain"));
            Assert.That(comp.ExcludedJobs.Select(x => x.Id), Does.Not.Contain("IAA"));
            Assert.That(comp.ExcludedJobs.Select(x => x.Id), Does.Not.Contain("BlueShieldOfficer"));
            Assert.That(comp.ProtectedJobs.Select(x => x.Id), Is.EquivalentTo(new[] { "Captain", "IAA", "BlueShieldOfficer" }));
            Assert.That(comp.ProtectedJobsAccess.Id, Is.EqualTo("CentralCommand"));

            // Declaring someone wanted is Captain/HoS only - narrower than FullAccess (which also
            // includes the HoP).
            Assert.That(comp.DeclareWantedAccess.Select(x => x.Id), Is.EquivalentTo(new[] { "Captain", "HeadOfSecurity" }));
        });

        await pair.CleanReturnAsync();
    }
}
