using System.Collections.Generic;
using System.Linq;
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
    private const string Map = "CharacterSelectionTestMap";

    // this game mode attempts to make everyone a traitor
    private const string TraitorsMode = "OopsAllTraitors";

    [TestPrototypes]
    private static readonly string Prototypes = $@"
- type: entity
  parent: BaseTraitorRule
  id: {TraitorsMode}
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
  id: {TraitorsMode}
  name: traitor-title
  description: traitor-description
  showInVote: false
  rules:
  - {TraitorsMode}

- type: gameMap
  id: {Map}
  mapName: {Map}
  mapPath: /Maps/Test/empty.yml
  minPlayers: 0
  stations:
    Empty:
      mapNameTemplate: {Map}
      stationProto: StandardNanotrasenStation
      components:
        - type: StationJobs
          availableJobs:
            Captain: [ 1, 1 ]
            Passenger: [ -1, -1 ]
            Mime: [ 1, 1 ]
";

    // some constants to help test case readability & also make the compiler catch typos
    private static readonly ProtoId<JobPrototype> Captain = "Captain";
    private static readonly ProtoId<JobPrototype> Passenger = "Passenger";
    private static readonly ProtoId<JobPrototype> Mime = "Mime";
    private static readonly ProtoId<JobPrototype> Clown = "Clown";
    private static readonly ProtoId<AntagPrototype> Traitor = "Traitor";

    // helper structs for test case definition readability
    public sealed class TestCharacter
    {
        public List<ProtoId<JobPrototype>> Jobs;
        public bool IsTraitor;
        public bool ExpectToSpawn;
        public bool Enabled = true;

        public HumanoidCharacterProfile ToProfile()
        {
            return HumanoidCharacterProfile.Random()
                .AsEnabled(Enabled)
                .WithJobPreferences(Jobs)
                .WithAntagPreferences(IsTraitor ? [Traitor] : []);
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
        public int? SetSeed;

        public SelectionTestData WithCharacters(IEnumerable<TestCharacter> newCharacters)
        {
            return new SelectionTestData()
            {
                Description = Description,
                HighPrioJob = HighPrioJob,
                MediumPrioJobs = MediumPrioJobs,
                LowPrioJobs = LowPrioJobs,
                Characters = newCharacters.ToList(),
                ExpectedJob = ExpectedJob,
                ExpectTraitor = ExpectTraitor,
                SetSeed = SetSeed,
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
                new() { Jobs = [ Passenger ], IsTraitor = true, ExpectToSpawn = true}
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
                new() { Jobs = [ Clown ], IsTraitor = true }
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
        // Since the game mode tries to make every player an antag, Captain should be excluded even if traitor is
        // selected on the Captain character.
        new()
        {
            Description = "Antag chars, one command",
            MediumPrioJobs = [ Captain, Passenger ],
            Characters =
            [
                new() { Jobs = [ Captain ], IsTraitor = true },
                new() { Jobs = [ Passenger ], IsTraitor = true, ExpectToSpawn = true }
            ],
            ExpectedJob = Passenger,
            ExpectTraitor = true
        },
        // also adapted from https://github.com/space-wizards/space-station-14/pull/36493#issuecomment-2926983119
        // BUT the behaviour described there was later changed as described in case 1 in
        // https://github.com/space-wizards/space-station-14/pull/36493#issuecomment-3014257219,
        // so this tests the updated behaviour
        // Since this player has no enabled characters that are eligible to be a traitor, their session will not be
        // selected to be an antag, and thus the Captain character will be eligible to spawn, even if the Captain
        // character has traitor enabled.
        new()
        {
            Description = "Only antag captain enabled",
            MediumPrioJobs = [ Captain, Passenger ],
            Characters =
            [
                new() { Jobs = [ Captain ], IsTraitor = true, ExpectToSpawn = true },
                new() { Jobs = [ Passenger ], IsTraitor = true, Enabled = false }
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
                new() { Jobs = [ Captain ], IsTraitor = true, ExpectToSpawn = true }
            ],
            ExpectedJob = Captain,
            ExpectTraitor = false
        },
        // Case 2 from https://github.com/space-wizards/space-station-14/pull/36493#issuecomment-3014257219
        // a player that is selected as an antag should always roll a character & job compatible with that
        // antag selection
        // Put another way, a player should only be pre-selected to be an antag if they have characters to be eligible
        // to be an antag. Thus if a player is pre-selected to be an antag, they should then only be eligible for the
        // set of antag-compatible jobs made from enabled characters that have that antag enabled.
        new()
        {
            Description = "Many chars, one antag",
            MediumPrioJobs = [ Mime, Passenger ],
            Characters =
            [
                new() { Jobs = [ Mime ], IsTraitor = true, ExpectToSpawn = true },
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
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, Map);

        var ticker = pair.Server.System<GameTicker>();

        ticker.SetGamePreset(TraitorsMode);

        var cPref = pair.Client.ResolveDependency<IClientPreferencesManager>();

        if(data.SetSeed is not null)
        {
            pair.Server.ResolveDependency<IRobustRandom>().SetSeed(data.SetSeed.Value);
        }

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

    // Run multiple round starts with the same set of characters, all of which are valid to select,
    // and verify that which character is selected varies
    // This uses a random seed and theoretically has a chance of randomly failing naturally.
    // The probability of this test failing is (1 - 1/c)^n where c is the number of characters and n is the maximum
    // number of rounds to run.
    // Currently this test is set to 4 characters, with a max of 48 rounds, making the probability of
    // failing one in a million. If you manage this, go buy a lottery ticket.
    [Test]
    public async Task VarietyTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, Map);

        var ticker = pair.Server.System<GameTicker>();

        ticker.SetGamePreset(TraitorsMode);

        var cPref = pair.Client.ResolveDependency<IClientPreferencesManager>();

        await pair.ReallyBeIdle();

        await pair.Client.WaitAssertion(() =>
        {
            cPref.UpdateJobPriorities( new()
            {
                { Passenger, JobPriority.Medium },
                { Mime, JobPriority.Medium },
            });
            cPref.CreateCharacter(HumanoidCharacterProfile.Random().AsEnabled().WithJobPreferences([Passenger]));
            cPref.CreateCharacter(HumanoidCharacterProfile.Random().AsEnabled().WithJobPreferences([Passenger]));
            cPref.CreateCharacter(HumanoidCharacterProfile.Random().AsEnabled().WithJobPreferences([Passenger, Mime]));
            cPref.CreateCharacter(HumanoidCharacterProfile.Random().AsEnabled().WithJobPreferences([Mime]));
        });

        HashSet<int> selectedCharacterSlots = new();
        var humanoidAppearanceSystem = pair.Server.System<HumanoidAppearanceSystem>();

        foreach (var i in Enumerable.Range(0, 48))
        {
            await pair.ReallyBeIdle();

            Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value],
                Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
            ticker.ToggleReadyAll(true);
            Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.ReadyToPlay));
            await pair.Server.WaitPost(() => ticker.StartRound());
            await pair.RunTicksSync(30);

            Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.JoinedGame));
            var baseProfile = humanoidAppearanceSystem.GetBaseProfile(pair.Player!.AttachedEntity.Value);
            var foundSlot = cPref.Preferences.Characters.FirstOrNull(kvp => kvp.Value.MemberwiseEquals(baseProfile))?.Key;

            Assert.That(foundSlot, Is.Not.Null);
            selectedCharacterSlots.Add(foundSlot.Value);

            await pair.Server.WaitPost(() => ticker.RestartRound());

            if (selectedCharacterSlots.Count == cPref.Preferences.Characters.Count)
            {
                break;
            }
        }
        Assert.That(selectedCharacterSlots, Has.Count.EqualTo(cPref.Preferences.Characters.Count));

        await pair.CleanReturnAsync();
    }

}
