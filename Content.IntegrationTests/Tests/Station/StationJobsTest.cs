using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Maps;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Station;

[TestFixture]
[TestOf(typeof(StationJobsSystem))]
public sealed class StationJobsTest : GameTest
{
    private const string StationMapId = "FooStation";
    private const string SecondStationMapId = "BarStation";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: playTimeTracker
  id: PlayTimeDummyAssistant

- type: playTimeTracker
  id: PlayTimeDummyMime

- type: playTimeTracker
  id: PlayTimeDummyClown

- type: playTimeTracker
  id: PlayTimeDummyCaptain

- type: playTimeTracker
  id: PlayTimeDummyChaplain

- type: department
  id: StationJobsTestDepartment
  name: department-Cargo
  description: department-Cargo-description
  color: ""#FFFFFF""
  roles:
  - TCaptain
  - TChaplain

- type: jobWeight
  id: StationJobsTest
  weights:
    TAssistant: 0
    TMime: 20
    TClown: -10
    TCaptain: 10
    TChaplain: 0

- type: gameMap
  id: {StationMapId}
  minPlayers: 0
  mapName: {StationMapId}
  mapPath: /Maps/Test/empty.yml
  jobWeights: StationJobsTest
  stations:
    Station:
      mapNameTemplate: {StationMapId}
      stationProto: StandardNanotrasenStation
      components:
        - type: StationJobs
          availableJobs:
            TMime: [0, -1]
            TAssistant: [-1, -1]
            TCaptain: [5, 5]
            TClown: [5, 6]

- type: jobWeight
  id: StationJobsBarTest
  weights:
    TCaptain: 30
    TChaplain: 20
    TMime: 100
    TAssistant: 0
    TClown: 0

- type: gameMap
  id: {SecondStationMapId}
  minPlayers: 0
  mapName: {SecondStationMapId}
  mapPath: /Maps/Test/empty.yml
  jobWeights: StationJobsBarTest
  stations:
    First:
      mapNameTemplate: First
      stationProto: StandardNanotrasenStation
      components:
        - type: StationJobs
          availableJobs:
            TCaptain: [1, 1]
            TChaplain: [1, 1]
            TAssistant: [0, 1]
            TClown: [-1, -1]
    Second:
      mapNameTemplate: Second
      stationProto: StandardNanotrasenStation
      components:
        - type: StationJobs
          availableJobs:
            TMime: [1, 1]

- type: job
  id: TAssistant
  playTimeTracker: PlayTimeDummyAssistant

- type: job
  id: TMime
  playTimeTracker: PlayTimeDummyMime

- type: job
  id: TClown
  playTimeTracker: PlayTimeDummyClown

- type: job
  id: TCaptain
  playTimeTracker: PlayTimeDummyCaptain

- type: job
  id: TChaplain
  playTimeTracker: PlayTimeDummyChaplain
";

    [Test]
    public async Task AssignJobsTest()
    {
        var pair = Pair;
        var server = pair.Server;

        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var barStationProto = prototypeManager.Index<GameMapPrototype>(SecondStationMapId);
        var entSysMan = server.ResolveDependency<IEntityManager>().EntitySysManager;
        var stationJobs = entSysMan.GetEntitySystem<StationJobsSystem>();
        var stationSystem = entSysMan.GetEntitySystem<StationSystem>();

        var firstStation = EntityUid.Invalid;
        var secondStation = EntityUid.Invalid;
        await server.WaitPost(() =>
        {
            firstStation = stationSystem.InitializeNewStation(
                barStationProto.Stations["First"], null, "First", barStationProto);
            secondStation = stationSystem.InitializeNewStation(
                barStationProto.Stations["Second"], null, "Second", barStationProto);
        });

        var dummies = await server.AddDummySessions(5);
        await server.WaitAssertion(() =>
        {
            var fakePlayers = new Dictionary<NetUserId, HumanoidCharacterProfile>
            {
                // The first station's captain minimum wins despite a lower player preference and the second station's
                // mime having the highest weight. This verifies both role weighting and station-by-station allocation.
                [dummies[0].UserId] = HumanoidCharacterProfile.Random()
                    .WithJobPriority("TCaptain", JobPriority.Low)
                    .WithJobPriority("TChaplain", JobPriority.High)
                    .WithJobPriority("TMime", JobPriority.Medium),
                [dummies[1].UserId] = HumanoidCharacterProfile.Random()
                    .WithJobPriority("TChaplain", JobPriority.High),
                // The second station's minimum must be assigned before the first station's optional assistant slot.
                [dummies[2].UserId] = HumanoidCharacterProfile.Random()
                    .WithJobPriority("TAssistant", JobPriority.High)
                    .WithJobPriority("TClown", JobPriority.Low),
                [dummies[3].UserId] = HumanoidCharacterProfile.Random()
                    .WithJobPriority("TAssistant", JobPriority.High)
                    .WithJobPriority("TMime", JobPriority.Low),
                [dummies[4].UserId] = HumanoidCharacterProfile.Random()
                    .WithJobPriorities(Array.Empty<KeyValuePair<ProtoId<JobPrototype>, JobPriority>>()),
            };

            var stations = new[] { firstStation, secondStation };
            var assigned = stationJobs.AssignJobs(fakePlayers, stations);
            stationJobs.AssignOverflowJobs(ref assigned, fakePlayers.Keys, fakePlayers, stations);

            Assert.Multiple(() =>
            {
                Assert.That(assigned[dummies[0].UserId], Is.EqualTo(((ProtoId<JobPrototype>?) "TCaptain", firstStation)));
                Assert.That(assigned[dummies[1].UserId], Is.EqualTo(((ProtoId<JobPrototype>?) "TChaplain", firstStation)));
                Assert.That(assigned[dummies[2].UserId], Is.EqualTo(((ProtoId<JobPrototype>?) "TAssistant", firstStation)));
                Assert.That(assigned[dummies[3].UserId], Is.EqualTo(((ProtoId<JobPrototype>?) "TMime", secondStation)));
                Assert.That(assigned[dummies[4].UserId], Is.EqualTo(((ProtoId<JobPrototype>?) "TClown", firstStation)));
            });
        });
    }

    [Test]
    public async Task MinimumJobsUseConfiguredFallback()
    {
        var pair = Pair;
        var server = pair.Server;
        var configuration = server.ResolveDependency<IConfigurationManager>();
        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var barStationProto = prototypeManager.Index<GameMapPrototype>(SecondStationMapId);
        var entSysMan = server.ResolveDependency<IEntityManager>().EntitySysManager;
        var stationJobs = entSysMan.GetEntitySystem<StationJobsSystem>();
        var stationSystem = entSysMan.GetEntitySystem<StationSystem>();
        var station = EntityUid.Invalid;

        await server.WaitPost(() =>
        {
            station = stationSystem.InitializeNewStation(
                barStationProto.Stations["First"], null, "First", barStationProto);
        });

        var dummies = await server.AddDummySessions(2);
        var sameDepartmentDummy = dummies[0];
        var noPreferenceDummy = dummies[1];
        var sameDepartmentProfiles = new Dictionary<NetUserId, HumanoidCharacterProfile>
        {
            [sameDepartmentDummy.UserId] = new HumanoidCharacterProfile()
                .WithJobPriority("TChaplain", JobPriority.Low),
        };

        var noPreferenceProfiles = new Dictionary<NetUserId, HumanoidCharacterProfile>
        {
            [noPreferenceDummy.UserId] = new HumanoidCharacterProfile()
                .WithJobPriorities(Array.Empty<KeyValuePair<ProtoId<JobPrototype>, JobPriority>>()),
        };

        var anyEligibleProfiles = new Dictionary<NetUserId, HumanoidCharacterProfile>
        {
            [sameDepartmentDummy.UserId] = sameDepartmentProfiles[sameDepartmentDummy.UserId],
            [noPreferenceDummy.UserId] = noPreferenceProfiles[noPreferenceDummy.UserId],
        };

        var originalValue = configuration.GetCVar(CCVars.GameMinimumJobFallback);
        try
        {
            await server.WaitAssertion(() =>
            {
                configuration.SetCVar(CCVars.GameMinimumJobFallback, MinimumJobFallback.SameDepartment);
                var sameDepartmentAssignments = stationJobs.AssignJobs(sameDepartmentProfiles, [station]);
                Assert.That(sameDepartmentAssignments[sameDepartmentDummy.UserId].Item1, Is.EqualTo((ProtoId<JobPrototype>?) "TCaptain"));

                configuration.SetCVar(CCVars.GameMinimumJobFallback, MinimumJobFallback.AnyEligiblePlayer);
                var anyEligibleAssignments = stationJobs.AssignJobs(anyEligibleProfiles, [station]);
                Assert.That(anyEligibleAssignments[sameDepartmentDummy.UserId].Item1, Is.EqualTo((ProtoId<JobPrototype>?) "TCaptain"));
                Assert.That(anyEligibleAssignments[noPreferenceDummy.UserId].Item1, Is.EqualTo((ProtoId<JobPrototype>?) "TChaplain"));

                configuration.SetCVar(CCVars.GameMinimumJobFallback, MinimumJobFallback.None);
                var noFallbackAssignments = stationJobs.AssignJobs(noPreferenceProfiles, [station]);
                Assert.That(noFallbackAssignments, Is.Empty);
            });
        }
        finally
        {
            await server.WaitPost(() =>
                configuration.SetCVar(CCVars.GameMinimumJobFallback, originalValue));
        }
    }

    [Test]
    public async Task AdjustJobsTest()
    {
        var pair = Pair;
        var server = pair.Server;

        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var fooStationProto = prototypeManager.Index<GameMapPrototype>(StationMapId);
        var entSysMan = server.ResolveDependency<IEntityManager>().EntitySysManager;
        var stationJobs = entSysMan.GetEntitySystem<StationJobsSystem>();
        var stationSystem = entSysMan.GetEntitySystem<StationSystem>();

        var station = EntityUid.Invalid;
        await server.WaitPost(() =>
        {
            station = stationSystem.InitializeNewStation(fooStationProto.Stations["Station"], null, $"Foo Station", fooStationProto);
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            // Verify jobs are/are not unlimited.
            Assert.Multiple(() =>
            {
                Assert.That(stationJobs.IsJobUnlimited(station, "TAssistant"), "TAssistant is expected to be unlimited.");
                Assert.That(stationJobs.IsJobUnlimited(station, "TMime"), "TMime is expected to be unlimited.");
                Assert.That(!stationJobs.IsJobUnlimited(station, "TCaptain"), "TCaptain is expected to not be unlimited.");
                Assert.That(!stationJobs.IsJobUnlimited(station, "TClown"), "TClown is expected to not be unlimited.");
            });
            Assert.Multiple(() =>
            {
                Assert.That(stationJobs.TrySetJobSlot(station, "TClown", 0), "Could not set TClown to have zero slots.");
                Assert.That(stationJobs.TryGetJobSlot(station, "TClown", out var clownSlots), "Could not get the number of TClown slots.");
                Assert.That(clownSlots, Is.EqualTo(0));
                Assert.That(!stationJobs.TryAdjustJobSlot(station, "TCaptain", -9999), "Was able to adjust TCaptain by -9999 without clamping.");
                Assert.That(stationJobs.TryAdjustJobSlot(station, "TCaptain", -9999, false, true), "Could not adjust TCaptain by -9999.");
                Assert.That(stationJobs.TryGetJobSlot(station, "TCaptain", out var captainSlots), "Could not get the number of TCaptain slots.");
                Assert.That(captainSlots, Is.EqualTo(0));
            });
            Assert.Multiple(() =>
            {
                Assert.That(stationJobs.TrySetJobSlot(station, "TChaplain", 10, true), "Could not create 10 TChaplain slots.");
                stationJobs.MakeJobUnlimited(station, "TChaplain");
                Assert.That(stationJobs.IsJobUnlimited(station, "TChaplain"), "Could not make TChaplain unlimited.");
            });
        });
    }

    [Test]
    public async Task InvalidRoundstartJobsTest()
    {
        var pair = Pair;
        var server = pair.Server;

        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var compFact = server.ResolveDependency<IComponentFactory>();
        var name = compFact.GetComponentName<StationJobsComponent>();

        await server.WaitAssertion(() =>
        {
            // invalidJobs contains all the jobs which can't be set for preference:
            // i.e. all the jobs that shouldn't be available round-start.
            var invalidJobs = new HashSet<string>();
            foreach (var job in prototypeManager.EnumeratePrototypes<JobPrototype>())
            {
                if (!job.SetPreference)
                    invalidJobs.Add(job.ID);
            }

            Assert.Multiple(() =>
            {
                foreach (var gameMap in prototypeManager.EnumeratePrototypes<GameMapPrototype>())
                {
                    foreach (var (stationId, station) in gameMap.Stations)
                    {
                        if (!station.StationComponentOverrides.TryGetComponent(name, out var comp))
                            continue;

                        foreach (var (job, array) in ((StationJobsComponent) comp).SetupAvailableJobs)
                        {
                            Assert.That(array.Length, Is.EqualTo(2));
                            Assert.That(array[0] is -1 or >= 0);
                            Assert.That(array[1] is -1 or >= 0);
                            if (array[0] >= 0 && array[1] >= 0)
                                Assert.That(array[0], Is.LessThanOrEqualTo(array[1]), "Round-start minimum exceeds maximum slots.");
                            Assert.That(invalidJobs, Does.Not.Contain(job), $"Station {stationId} contains job prototype {job} which cannot be present roundstart.");
                        }
                    }
                }
            });
        });
    }
}
