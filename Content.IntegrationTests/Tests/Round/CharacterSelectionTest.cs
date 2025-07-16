using System.Collections.Generic;
using System.Linq;
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

    private static readonly string _map = "CharacterSelectionTestMap";

    // this testing game mode attempts to make everyone a traitor
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

        public HumanoidCharacterProfile ToProfile()
        {
            var profile = HumanoidCharacterProfile.Random().AsEnabled();
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
                HighPrioJob = HighPrioJob,
                MediumPrioJobs = MediumPrioJobs,
                LowPrioJobs = LowPrioJobs,
                Characters = new_characters.ToList(),
                ExpectedJob = ExpectedJob,
                ExpectTraitor = ExpectTraitor
            };
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

    public static readonly List<SelectionTestData> SelectionTestCaseData =
    [
        // a player with one character & one job, traitor enabled, should spawn as that job and as a traitor
        // (note that the game mode used for these tests attempts to make every player a traitor)
        new()
        {
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
            HighPrioJob = Clown,
            Characters =
            [
                new() { Jobs = [ Clown ], Traitor = true }
            ]
        },
        // Case 1 from https://github.com/space-wizards/space-station-14/pull/36493#issuecomment-3014257219
        // if a player has no antag-compatible jobs enabled they should not be selected as an antag
        new() {
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

    // use a function for test data so we can use classes
    public static IEnumerable<SelectionTestData> SelectionTestCases()
    {
        foreach (var testCaseData in SelectionTestCaseData)
        {
            yield return testCaseData;

            // test different orders of characters with the same rng seed to minimize effects of rng on tests
            // (the rng seed is set in SelectionTest())
            if (testCaseData.Characters.Count > 1)
            {
                var reversedCharacters = testCaseData.Characters.ShallowClone();
                reversedCharacters.Reverse();
                yield return testCaseData.WithCharacters(reversedCharacters);
            }

            if (testCaseData.Characters.Count > 2)
            {
                var rotatedCharacters = testCaseData.Characters.ShallowClone();
                for (var i = 1; i < testCaseData.Characters.Count; i++)
                {
                    var movingCharacter = rotatedCharacters[0];
                    rotatedCharacters.RemoveAt(0);
                    rotatedCharacters.Add(movingCharacter);
                    yield return testCaseData.WithCharacters(rotatedCharacters);
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

}
