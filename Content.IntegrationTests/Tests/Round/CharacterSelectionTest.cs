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
";

    // a few little helper structs for test case definition readability
    public sealed class TestJobPriorities
    {
        public JobPriority Captain;
        public JobPriority Passenger;

        public Dictionary<ProtoId<JobPrototype>, JobPriority> ToDictForPreferences()
        {
            var dict = new Dictionary<ProtoId<JobPrototype>, JobPriority>();
            if (Captain != JobPriority.Never)
            {
                dict.Add("Captain", Captain);
            }
            if (Passenger != JobPriority.Never)
            {
                dict.Add("Passenger", Passenger);
            }
            return dict;
        }
    }

    public sealed class TestCharacter
    {
        public bool Captain;
        public bool Passenger;
        public bool Traitor;

        public HumanoidCharacterProfile ToProfile()
        {
            var profile = HumanoidCharacterProfile.Random().AsEnabled();
            if (Captain)
            {
                profile = profile.WithJob("Captain");
            }
            // default job set has passenger already
            if (!Passenger)
            {
                profile = profile.WithoutJob("Passenger");
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
        public TestCharacter ExpectedCharacter;    // null if player should not be spawned
        public List<TestCharacter> OtherCharacters = [];
        public string ExpectedJobName = "";
        public bool ExpectTraitor;
    }

    // use a function for test data so we can use classes
    public static IEnumerable<SelectionTestData> SelectionTestCases()
    {
        yield return new SelectionTestData()
        {
            JobPriorities = new TestJobPriorities(){Passenger = JobPriority.High},
            ExpectedCharacter = new TestCharacter(){Passenger = true, Traitor = true},
            ExpectedJobName = "Passenger",
            ExpectTraitor = true
        };
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

        await pair.ReallyBeIdle();

        HumanoidCharacterProfile expectedCharacterProfile = null;

        await pair.Client.WaitAssertion(() =>
        {
            if (data.ExpectedCharacter != null)
            {
                expectedCharacterProfile = data.ExpectedCharacter.ToProfile();
                cPref.CreateCharacter(expectedCharacterProfile);
            }

            foreach (var character in data.OtherCharacters)
            {
                cPref.CreateCharacter(character.ToProfile());
            }

            // delete initial default character now that there are other character(s) so it won't
            // just be immediately re-created
            cPref.DeleteCharacter(0);

            cPref.UpdateJobPriorities(data.JobPriorities.ToDictForPreferences());
        });

        await pair.ReallyBeIdle();

        Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
        ticker.ToggleReadyAll(true);
        Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.ReadyToPlay));
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(30);

        if (expectedCharacterProfile == null)
        {
            Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
        }
        else
        {
            pair.AssertJob(data.ExpectedJobName, pair.Player!);
            var humanoidAppearanceSystem = pair.Server.System<HumanoidAppearanceSystem>();
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
            // TODO: check correct profile spawned
            // and maybe check status??
        }

        await pair.Server.WaitPost(() => ticker.RestartRound());
        await pair.CleanReturnAsync();
    }

}
