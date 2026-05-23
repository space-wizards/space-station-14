#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Objectives.Commands;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Commands;

public sealed class ObjectiveCommandsTest : GameTest
{

    private const string ObjectiveProtoId = "MindCommandsTestObjective";

    [TestPrototypes]
    private const string Prototypes = $"""
- type: entity
  id: {ObjectiveProtoId}
  components:
  - type: Objective
    difficulty: 1
    issuer: objective-issuer-syndicate
    icon:
      sprite: error.rsi
      state: error
  - type: DieCondition
""";

    [SidedDependency(Side.Server)] private SharedMindSystem _sMindSystem = null!;

    /// <summary>
    /// Tests using <c>addobjective</c>, <c>lsobjectives</c>,
    /// and <c>rmobjective</c> on the player.
    /// </summary>
    [TestOf(typeof(AddObjectiveCommand))]
    [TestOf(typeof(ListObjectivesCommand))]
    [TestOf(typeof(RemoveObjectiveCommand))]
    [Test]
    [Description("Checks that the addobjective, lsobjectives, and rmobjective commands work as expected.")]
    public async Task AddListRemoveObjectiveTest()
    {
        Entity<MindComponent> mind = default!;

        await Server.WaitPost(() =>
        {
            mind = _sMindSystem.GetOrCreateMind(ServerSession!.UserId);
        });

        Assert.That(mind.Comp!.Objectives, Is.Empty, "Player started with objectives.");

        await Pair.WaitCommand($"addobjective {ServerSession!.Name} {ObjectiveProtoId}");

        Assert.That(mind.Comp.Objectives, Has.Count.EqualTo(1), "addobjective failed to increase Objectives count.");

        await Pair.WaitCommand($"lsobjectives {ServerSession.Name}");

        // Nothing really to assert here; but at least we're running the code and checking for errors!

        await Pair.WaitCommand($"rmobjective {ServerSession.Name} 0");

        Assert.That(mind.Comp.Objectives, Is.Empty, "rmobjective failed to remove objective");
    }
}
