#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Mind;
using Content.Server.Objectives.Components;
using Content.Server.Revolutionary.Components;
using Content.Server.Roles;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Objectives;

public sealed class KillObjectiveTest : GameTest
{

    private const string KillRandomHeadObjective = "TestKillRandomHeadObjective";
    private const string KillRandomPersonObjective = "TestKillRandomPersonObjective";
    private const string KillStationAiObjective = "TestKillStationAiObjective";

    [TestPrototypes]
    private const string Prototypes = $"""
- type: entity
  id: {KillRandomPersonObjective}
  description: Do it however you like, just make sure they don't get off the station.
  components:
  - type: Objective
    issuer: TheSyndicate
    difficulty: 1
  - type: TargetObjective
    title: objective-condition-maroon-person-title
  - type: PickRandomPerson
    conditions:
    - !type:AntagonistTagCondition # Only roll on-station targets.
      tags:
      - OnStation
    - !type:AntagonistTagCondition # Don't roll unkillable targets.
      inverted: true
      tags:
      - Unkillable
    # Can't have multiple objectives to kill the same person.
    - !type:ObjectiveTargetCondition
      inverted: true
      whitelist:
        components:
        - KillPersonCondition
  - type: KillPersonCondition
    requireMaroon: true

- type: entity
  id: {KillRandomHeadObjective}
  description: We need this head gone and you probably know why. Make sure they don't make it to CentComm, even if they're dead. Good luck, agent.
  components:
  - type: Objective
    issuer: TheSyndicate
    difficulty: 1
  - type: TargetObjective
    title: objective-condition-kill-maroon-title
  - type: PickRandomPerson
    conditions:
    - !type:AntagonistTagCondition # Only roll on-station targets.
      tags:
      - OnStation
    - !type:AntagonistTagCondition # Don't roll unkillable targets.
      inverted: true
      tags:
      - Unkillable
    # Can't have multiple objectives to kill the same person.
    - !type:ObjectiveTargetCondition
      inverted: true
      whitelist:
        components:
        - KillPersonCondition
    - !type:BodyWhitelistCondition
      whitelist:
        components:
        - CommandStaff
  - type: KillPersonCondition
    requireDead: true
    requireMaroon: true

- type: entity
  id: {KillStationAiObjective}
  description: Nanotrasen proudly boasts about their state of the art artificial intelligence. Remind them it's just another toy that can be broken.
  components:
  - type: Objective
    issuer: TheSyndicate
    difficulty: 1
  - type: TargetObjective
    title: objective-condition-kill-station-ai
  - type: PickRandomPerson
    pool: !type:AliveAiPool
    conditions:
    - !type:AntagonistTagCondition # Only roll on-station targets.
      tags:
      - OnStation
    - !type:AntagonistTagCondition # Don't roll unkillable targets.
      inverted: true
      tags:
      - Unkillable
    # Can't have multiple objectives to kill the same person.
    - !type:ObjectiveTargetCondition
      inverted: true
      whitelist:
        components:
        - KillPersonCondition
  - type: KillPersonCondition
    requireDead: true
""";

    public override PoolSettings PoolSettings => new()
    {
        Connected = false
    };

    [SidedDependency(Side.Server)] private MindSystem _sMindSystem = null!;
    [SidedDependency(Side.Server)] private RoleSystem _sRoleSystem = null!;
    [SidedDependency(Side.Server)] private SharedMapSystem _sMapSystem = null!;
    [SidedDependency(Side.Server)] private SharedContainerSystem _sContainerSystem = null!;

    private static readonly EntProtoId MindRoleTraitor = "MindRoleTraitor";
    private static readonly EntProtoId MobHuman = "MobHuman";
    private static readonly EntProtoId PlayerStationAiEmpty = "PlayerStationAiEmpty";
    private static readonly EntProtoId StationAiBrain = "StationAiBrain";
    private static readonly ProtoId<JobPrototype> PassengerJob = "Passenger";
    private static readonly ProtoId<JobPrototype> CaptainJob = "Captain";
    private static readonly ProtoId<JobPrototype> StationAiJob = "StationAi";

    [Test]
    [RunOnSide(Side.Server)]
    [Description("Tests that the game can properly assign kill objectives")]
    public async Task TestTraitorKillObjectiveTargets()
    {
        var map = _sMapSystem.CreateMap();
        var coordinates = new EntityCoordinates(map, System.Numerics.Vector2.Zero);

        var traitor = CreateMind(SSpawnAtPosition(MobHuman, coordinates), PassengerJob);
        _sRoleSystem.MindAddRole(traitor.Owner, MindRoleTraitor, silent: true);

        var captainBody = SSpawnAtPosition(MobHuman, coordinates);
        SEntMan.EnsureComponent<CommandStaffComponent>(captainBody);
        var captain = CreateMind(captainBody, CaptainJob);

        var tider = CreateMind(SSpawnAtPosition(MobHuman, coordinates), PassengerJob);

        var core = SSpawnAtPosition(PlayerStationAiEmpty, coordinates);
        var brain = SSpawnAtPosition(StationAiBrain, coordinates);
        var container = _sContainerSystem.GetContainer(core, StationAiHolderComponent.Container);
        Assume.That(_sContainerSystem.Insert(brain, container), Is.True, "Could not insert the AI into its core.");
        var ai = CreateMind(brain, StationAiJob);

        Assert.Multiple(() =>
        {
            //Assign head first because we can always expect that to be captain given the options
            Assert.That(_sMindSystem.TryAddObjective(traitor.Owner, traitor.Comp, KillRandomHeadObjective), Is.True,
                $"Could not assign {KillRandomHeadObjective}.");
            //Tider is only remaining valid target for a kill person objective.
            Assert.That(_sMindSystem.TryAddObjective(traitor.Owner, traitor.Comp, KillRandomPersonObjective), Is.True,
                $"Could not assign {KillRandomPersonObjective}.");
            //Also check AI, comment because it feels weird having them for the other two and not this
            Assert.That(_sMindSystem.TryAddObjective(traitor.Owner, traitor.Comp, KillStationAiObjective), Is.True,
                $"Could not assign {KillStationAiObjective}.");
        });
        Assert.Multiple(() =>
        {
            Assert.That(SComp<TargetObjectiveComponent>(traitor.Comp.Objectives[0]).Target, Is.EqualTo(captain.Owner),
                $"{KillRandomHeadObjective} selected the wrong target.");
            Assert.That(SComp<TargetObjectiveComponent>(traitor.Comp.Objectives[1]).Target, Is.EqualTo(tider.Owner),
                $"{KillRandomPersonObjective} selected the wrong target.");
            Assert.That(SComp<TargetObjectiveComponent>(traitor.Comp.Objectives[2]).Target, Is.EqualTo(ai.Owner),
                $"{KillStationAiObjective} selected the wrong target.");
        });
    }

    private Entity<MindComponent> CreateMind(EntityUid body, ProtoId<JobPrototype> job)
    {
        var mind = _sMindSystem.CreateMind(null);
        _sMindSystem.TransferTo(mind.Owner, body);
        _sRoleSystem.MindAddJobRole(mind.Owner, jobPrototype: job, silent: true);
        return mind;
    }
}
