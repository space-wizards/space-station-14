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

    // a few little helper structs for test case definition readability
    public sealed class TestJobPriorities
    {
        public JobPriority Captain;
        public JobPriority Mime;
        public JobPriority Passenger;
        public JobPriority Boxer;

        public Dictionary<ProtoId<JobPrototype>, JobPriority> ToDictForPreferences()
        {
            var dict = new Dictionary<ProtoId<JobPrototype>, JobPriority>();
            if (Captain != JobPriority.Never)
            {
                dict.Add("Captain", Captain);
            }
            if (Mime != JobPriority.Never)
            {
                dict.Add("Mime", Mime);
            }
            if (Passenger != JobPriority.Never)
            {
                dict.Add("Passenger", Passenger);
            }
            if (Boxer != JobPriority.Never)
            {
                dict.Add("Boxer", Boxer);
            }
            return dict;
        }
    }

    public sealed class TestCharacter
    {
        public bool Captain;
        public bool Mime;
        public bool Passenger;
        public bool Boxer;
        public bool Traitor;
        public bool ExpectToSpawn;

        public HumanoidCharacterProfile ToProfile()
        {
            var profile = HumanoidCharacterProfile.Random().AsEnabled();
            if (Captain)
            {
                profile = profile.WithJob("Captain");
            }
            if (Mime)
            {
                profile = profile.WithJob("Mime");
            }
            // default job set has passenger already
            if (!Passenger)
            {
                profile = profile.WithoutJob("Passenger");
            }
            if (Boxer)
            {
                profile = profile.WithJob("Boxer");
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
        public TestJobPriorities JobPriorities;
        public List<TestCharacter> Characters = [];
        public string ExpectedJobName = "";
        public bool ExpectTraitor;

        public SelectionTestData WithCharacters(IEnumerable<TestCharacter> new_characters)
        {
            return new SelectionTestData()
            {
                JobPriorities = JobPriorities,
                Characters = new_characters.ToList(),
                ExpectedJobName = ExpectedJobName,
                ExpectTraitor = ExpectTraitor
            };
        }
    }

    public static readonly List<SelectionTestData> SelectionTestCaseData =
    [
        new()
        {
            JobPriorities = new TestJobPriorities() { Passenger = JobPriority.High },
            Characters =
            [
                new TestCharacter() { Passenger = true, Traitor = true, ExpectToSpawn = true}
            ],
            ExpectedJobName = "Passenger",
            ExpectTraitor = true
        },
        new() // Case 1 from https://github.com/space-wizards/space-station-14/pull/36493#issuecomment-3014257219
        {
            JobPriorities = new TestJobPriorities() { Captain = JobPriority.High },
            Characters =
            [
                new TestCharacter() { Captain = true, Traitor = true, ExpectToSpawn = true }
            ],
            ExpectedJobName = "Captain",
            ExpectTraitor = false
        },
        new() // Case 2 from https://github.com/space-wizards/space-station-14/pull/36493#issuecomment-3014257219
        {
            JobPriorities = new TestJobPriorities() { Mime = JobPriority.Medium, Passenger = JobPriority.Medium },
            Characters =
            [
                new TestCharacter() { Mime = true, Traitor = true, ExpectToSpawn = true },
                new TestCharacter() { Passenger = true },
                new TestCharacter() { Passenger = true },
                new TestCharacter() { Passenger = true },
                new TestCharacter() { Passenger = true },
                new TestCharacter() { Passenger = true },
                new TestCharacter() { Captain = true }
            ],
            ExpectedJobName = "Mime",
            ExpectTraitor = true
        },
        new()
        {
            JobPriorities = new TestJobPriorities() { Boxer = JobPriority.High },
            Characters =
            [
                new TestCharacter() { Boxer = true, Traitor = true }
            ]
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

            cPref.UpdateJobPriorities(data.JobPriorities.ToDictForPreferences());
        });

        await pair.ReallyBeIdle();

    //    pair.Server.ResolveDependency<IRobustRandom>().SetSeed(6);
    //    pair.Client.ResolveDependency<IRobustRandom>().SetSeed(6);

        Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
        ticker.ToggleReadyAll(true);
        Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.ReadyToPlay));
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(50);

        if (expectedCharacterProfile == null)
        {
            Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.Not.EqualTo(PlayerGameStatus.JoinedGame));
        }
        else
        {
            Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.JoinedGame));
            pair.AssertJob(data.ExpectedJobName, pair.Player!);
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
