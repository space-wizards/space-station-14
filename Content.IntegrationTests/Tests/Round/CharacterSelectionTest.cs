using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Castle.Components.DictionaryAdapter.Xml;
using Content.Client.Lobby;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.Humanoid;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Round;

[TestFixture]
public sealed class CharacterSelectionTest
{
    // this map has slots for captain, mime, unlimited passengers, and no clowns
    private static readonly string _map = "CharacterSelectionTestMap";

    // this game mode attempts to make everyone a traitor
    private static readonly string _traitorsMode = "OopsAllTraitors";

    [TestPrototypes]
    private static readonly string Prototypes = $@"
- type: entity
  parent: BaseTraitorRule
  id: {_traitorsMode}
  components:
  - type: GameRule
    minPlayers: 0
    delay:
      min: 5
      max: 10
  - type: AntagSelection
    selectionTime: IntraPlayerSpawn
    definitions:
    - prefRoles: [ Traitor ]
      max: 99
      playerRatio: 1
      blacklist:
        components:
        - AntagImmune
      lateJoinAdditional: true
      mindRoles:
      - MindRoleTraitor

- type: gamePreset
  id: {_traitorsMode}
  name: traitor-title
  description: traitor-description
  showInVote: false
  rules:
  - {_traitorsMode}

- type: gameMap
  id: {_map}
  mapName: {_map}
  mapPath: /Maps/Test/empty.yml
  minPlayers: 0
  stations:
    Empty:
      mapNameTemplate: {_map}
      stationProto: StandardNanotrasenStation
      components:
        - type: StationJobs
          availableJobs:
            Captain: [ 1, 1 ]
            Passenger: [ -1, -1 ]
            Mime: [ 1, 1 ]
";

    // some constants to help test case readability & also make the compiler catch typos
    public static readonly ProtoId<JobPrototype> Captain = "Captain";
    public static readonly ProtoId<JobPrototype> Passenger = "Passenger";
    public static readonly ProtoId<JobPrototype> Mime = "Mime";
    public static readonly ProtoId<JobPrototype> Clown = "Clown";

    // helper structs for test case definition readability
    public sealed class TestCharacter
    {
        public List<ProtoId<JobPrototype>> Jobs;
        public bool Traitor;
        public bool ExpectToSpawn;
        public bool Enabled = true;

        public HumanoidCharacterProfile ToProfile()
        {
            var profile = HumanoidCharacterProfile.Random();

            if (Enabled)
            {
                profile = profile.AsEnabled();
            }

            // passenger is present by default, remove it first
            profile = profile.WithoutJob(Passenger);

            foreach (var job in Jobs)
            {
                profile = profile.WithJob(job);
            }

            if (Traitor)
            {
                profile = profile.WithAntagPreference("Traitor", true);
            }

            return profile;
        }
    }

    public sealed class SelectionTestData
    {
        public string Description;
        public ProtoId<JobPrototype>? HighPrioJob;
        public List<ProtoId<JobPrototype>> MediumPrioJobs = [];
        public List<ProtoId<JobPrototype>> LowPrioJobs = [];
        public List<TestCharacter> Characters = [];
        public ProtoId<JobPrototype>? ExpectedJob;
        public bool ExpectTraitor;

        public SelectionTestData WithCharacters(IEnumerable<TestCharacter> new_characters)
        {
            return new SelectionTestData()
            {
                Description = Description,
                HighPrioJob = HighPrioJob,
                MediumPrioJobs = MediumPrioJobs,
                LowPrioJobs = LowPrioJobs,
                Characters = new_characters.ToList(),
                ExpectedJob = ExpectedJob,
                ExpectTraitor = ExpectTraitor
            };
        }

        public TestCaseData ToTestCaseData()
        {
            return new TestCaseData(this).SetArgDisplayNames(Description);
        }

        public Dictionary<ProtoId<JobPrototype>, JobPriority> MakeJobPrioDict()
        {
            var dict = new Dictionary<ProtoId<JobPrototype>, JobPriority>();
            if (HighPrioJob.HasValue)
            {
                dict.Add(HighPrioJob.Value,  JobPriority.High);
            }
            foreach (var job in MediumPrioJobs)
            {
                dict.Add(job,  JobPriority.Medium);
            }
            foreach (var job in LowPrioJobs)
            {
                dict.Add(job,  JobPriority.Low);
            }
            return dict;
        }
    }

    // actual test case definitions for cases that should result in a specific character/job/traitor combination
    public static readonly List<SelectionTestData> SelectionTestCaseData =
    [
        // a player with one character & one job, traitor enabled, should spawn as that job and as a traitor
        // (note that the game mode used for these tests attempts to make every player a traitor)
        new()
        {
            Description = "Single char, antag",
            HighPrioJob = Passenger,
            Characters =
            [
                new() { Jobs = [ Passenger ], Traitor = true, ExpectToSpawn = true}
            ],
            ExpectedJob = Passenger,
            ExpectTraitor = true
        },
        // a player with only a job that doesn't appear on the station shouldn't spawn, even if they could be
        // selected as a traitor
        new()
        {
            Description = "Single char, unavailable job",
            HighPrioJob = Clown,
            Characters =
            [
                new() { Jobs = [ Clown ], Traitor = true }
            ]
        },
        // disabled characters should be ignored
        new()
        {
            Description = "Many chars, one enabled",
            MediumPrioJobs = [Passenger, Mime],
            Characters =
            [
                new() { Jobs = [ Mime ], ExpectToSpawn = true },
                new() { Jobs = [ Passenger ], Enabled = false },
                new() { Jobs = [ Mime ], Enabled = false },
                new() { Jobs = [ Captain ], Enabled = false },
                new() { Jobs = [ Passenger, Mime, Captain ], Enabled = false },
                new() { Jobs = [ Passenger, Mime, Captain ], Enabled = false }
            ],
            ExpectedJob = Mime
        },
        // High priority job should be chosen over medium priority job with same weight, then the character
        // with that job should spawn
        new()
        {
            Description = "Many chars, one with high prio job",
            HighPrioJob = Mime,
            MediumPrioJobs = [ Passenger ],
            Characters =
            [
                new() { Jobs = [ Mime ], ExpectToSpawn = true },
                new() { Jobs = [ Passenger ] },
                new() { Jobs = [ Passenger ] },
                new() { Jobs = [ Captain ] },
                new() { Jobs = [ Clown ] }
            ],
            ExpectedJob = Mime
        },
        // Medium priority job should be chosen over low priority job with same weight, then the character
        // with that job should spawn; job with no slots on station should have no effect
        new()
        {
            Description = "Many chars, one with expected medium prio job",
            HighPrioJob = Clown,
            MediumPrioJobs = [ Passenger ],
            LowPrioJobs = [ Mime ],
            Characters =
            [
                new() { Jobs = [ Passenger ], ExpectToSpawn = true },
                new() { Jobs = [ Mime ] },
                new() { Jobs = [ Captain ] },
                new() { Jobs = [ Clown ] }
            ],
            ExpectedJob = Passenger
        },
        // adapted from https://github.com/space-wizards/space-station-14/pull/36493#issuecomment-2926983119
        new()
        {
            Description = "Antag chars, one command",
            MediumPrioJobs = [ Captain, Passenger ],
            Characters =
            [
                new() { Jobs = [ Captain ], Traitor = true },
                new() { Jobs = [ Passenger ], Traitor = true, ExpectToSpawn = true }
            ],
            ExpectedJob = Passenger,
            ExpectTraitor = true
        },
        // also adapted from https://github.com/space-wizards/space-station-14/pull/36493#issuecomment-2926983119
        // BUT the behaviour described there was later changed as described in case 1 in
        // https://github.com/space-wizards/space-station-14/pull/36493#issuecomment-3014257219,
        // so this tests the updated behaviour
        new()
        {
            Description = "Only antag captain enabled",
            MediumPrioJobs = [ Captain, Passenger ],
            Characters =
            [
                new() { Jobs = [ Captain ], Traitor = true, ExpectToSpawn = true },
                new() { Jobs = [ Passenger ], Traitor = true, Enabled = false }
            ],
            ExpectedJob = Captain
        },
        // Case 1 from https://github.com/space-wizards/space-station-14/pull/36493#issuecomment-3014257219
        // if a player has no antag-compatible jobs enabled they should not be selected as an antag
        new() {
            Description = "Only antag captain",
            HighPrioJob = Captain,
            Characters =
            [
                new() { Jobs = [ Captain ], Traitor = true, ExpectToSpawn = true }
            ],
            ExpectedJob = Captain,
            ExpectTraitor = false
        },
        // Case 2 from https://github.com/space-wizards/space-station-14/pull/36493#issuecomment-3014257219
        // a player that is selected as an antag should always roll a character & job compatible with that
        // antag selection
        new()
        {
            Description = "Many chars, one antag",
            MediumPrioJobs = [ Mime, Passenger ],
            Characters =
            [
                new() { Jobs = [ Mime ], Traitor = true, ExpectToSpawn = true },
                new() { Jobs = [ Passenger ] },
                new() { Jobs = [ Passenger ] },
                new() { Jobs = [ Passenger ] },
                new() { Jobs = [ Passenger ] },
                new() { Jobs = [ Passenger ] },
                new() { Jobs = [ Captain ] }
            ],
            ExpectedJob = Mime,
            ExpectTraitor = true
        }
    ];

    // use a function for test data so we can use classes & also test different permutations of the same characters
    public static IEnumerable<TestCaseData> SelectionTestCases()
    {
        foreach (var testCase in SelectionTestCaseData)
        {
            yield return testCase.ToTestCaseData();

            // test different orders of characters with the same rng seed to minimize effects of rng on tests
            // (the rng seed is set in SelectionTest())
            if (testCase.Characters.Count > 1)
            {
                var reversedCharacters = testCase.Characters.ShallowClone();
                reversedCharacters.Reverse();
                yield return testCase.WithCharacters(reversedCharacters).ToTestCaseData();
            }

            if (testCase.Characters.Count > 2)
            {
                var rotatedCharacters = testCase.Characters.ShallowClone();
                for (var i = 1; i < testCase.Characters.Count; i++)
                {
                    var movingCharacter = rotatedCharacters[0];
                    rotatedCharacters.RemoveAt(0);
                    rotatedCharacters.Add(movingCharacter);
                    yield return testCase.WithCharacters(rotatedCharacters).ToTestCaseData();
                }
            }
        }
    }

    [Test]
    [TestCaseSource(nameof(SelectionTestCases))]
    public async Task SelectionTest(SelectionTestData data)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, _map);

        var ticker = pair.Server.System<GameTicker>();

        ticker.SetGamePreset(_traitorsMode);

        var cPref = pair.Client.ResolveDependency<IClientPreferencesManager>();
        pair.Server.ResolveDependency<IRobustRandom>().SetSeed(0);

        await pair.ReallyBeIdle();

        HumanoidCharacterProfile expectedCharacterProfile = null;

        await pair.Client.WaitAssertion(() =>
        {
            foreach (var character in data.Characters)
            {
                var profile = character.ToProfile();
                cPref.CreateCharacter(profile);
                if (character.ExpectToSpawn)
                {
                    expectedCharacterProfile = profile;
                }
            }

            // delete initial default character now that there are other character(s) so it won't
            // just be immediately re-created
            cPref.DeleteCharacter(0);

            cPref.UpdateJobPriorities(data.MakeJobPrioDict());
        });

        await pair.ReallyBeIdle();

        Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
        ticker.ToggleReadyAll(true);
        Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.ReadyToPlay));
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(30);

        if (expectedCharacterProfile == null)
        {
            Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.Not.EqualTo(PlayerGameStatus.JoinedGame));
        }
        else
        {
            Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.JoinedGame));
            pair.AssertJob(data.ExpectedJob.ToString(), pair.Player!);
            var antagSystem = pair.Server.System<AntagSelectionSystem>();
            var antags = antagSystem.GetPreSelectedAntagDefinitions(pair.Player);
            if (data.ExpectTraitor)
            {
                Assert.That(antags.Count, Is.EqualTo(1));
                Assert.That(antags.First().MindRoles.Count, Is.EqualTo(1));
                Assert.That(antags.First().MindRoles.First(), Is.EqualTo("MindRoleTraitor"));
            }
            else
            {
                Assert.That(antags.Count, Is.EqualTo(0));
            }
            var humanoidAppearanceSystem = pair.Server.System<HumanoidAppearanceSystem>();
            var spawnedProfile = humanoidAppearanceSystem.GetBaseProfile(pair.Player!.AttachedEntity.Value);
            Assert.That(spawnedProfile.MemberwiseEquals(expectedCharacterProfile), Is.True);
        }

        await pair.Server.WaitPost(() => ticker.RestartRound());
        await pair.CleanReturnAsync();
    }

    // run multiple round starts with the same set of characters, all of which are valid to select,
    // and verify that which character is selected varies
    [Test]
    public async Task VarietyTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, _map);

        var ticker = pair.Server.System<GameTicker>();

        ticker.SetGamePreset(_traitorsMode);

        var cPref = pair.Client.ResolveDependency<IClientPreferencesManager>();
        pair.Server.ResolveDependency<IRobustRandom>().SetSeed(0);

        await pair.ReallyBeIdle();

        await pair.Client.WaitAssertion(() =>
        {
            cPref.UpdateJobPriorities( new() {
                { Passenger, JobPriority.Medium },
                { Mime, JobPriority.Medium }
            });
            cPref.CreateCharacter(HumanoidCharacterProfile.Random().AsEnabled().WithJob(Passenger));
            cPref.CreateCharacter(HumanoidCharacterProfile.Random().AsEnabled().WithJob(Passenger));
            cPref.CreateCharacter(HumanoidCharacterProfile.Random().AsEnabled().WithJob(Mime));
            cPref.CreateCharacter(HumanoidCharacterProfile.Random().AsEnabled().WithoutJob(Passenger).WithJob(Mime));
        });

        HashSet<int> selectedCharacterSlots = new();
        var humanoidAppearanceSystem = pair.Server.System<HumanoidAppearanceSystem>();

        for (var i = 0; i < 20; i++)
        {
            await pair.ReallyBeIdle();

            Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value],
                Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
            ticker.ToggleReadyAll(true);
            Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.ReadyToPlay));
            await pair.Server.WaitPost(() => ticker.StartRound());
            await pair.RunTicksSync(30);

            Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.JoinedGame));
            bool foundSelectedSlot = false;
            foreach (var slot in cPref.Preferences.Characters.Keys)
            {
                if (cPref.Preferences.Characters[slot]
                    .MemberwiseEquals(humanoidAppearanceSystem.GetBaseProfile(pair.Player!.AttachedEntity.Value)))
                {
                    foundSelectedSlot = true;
                    selectedCharacterSlots.Add(slot);
                }
            }
            Assert.That(foundSelectedSlot, Is.True);

            await pair.Server.WaitPost(() => ticker.RestartRound());

            if (selectedCharacterSlots.Count == cPref.Preferences.Characters.Count)
            {
                break;
            }
        }
        Assert.That(selectedCharacterSlots.Count, Is.EqualTo(cPref.Preferences.Characters.Count));

        await pair.CleanReturnAsync();
    }

}
