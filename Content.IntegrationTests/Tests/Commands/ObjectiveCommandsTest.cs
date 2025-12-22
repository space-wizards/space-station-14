#nullable enable
using System.Linq;
using Content.Server.Objectives;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests.Commands;

public sealed class ObjectiveCommandsTest
{

    private const string ObjectiveProtoId = "MindCommandsTestObjective";
    private const string DummyUsername = "MindCommandsTestUser";

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

    /// <summary>
    /// Creates a dummy session, and assigns it a mind, then
    /// tests using <c>addobjective</c>, <c>lsobjectives</c>,
    /// and <c>rmobjective</c> on it.
    /// </summary>
    [Test]
    public async Task AddListRemoveObjectiveTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var playerMan = server.ResolveDependency<ISharedPlayerManager>();
        var mindSys = server.System<SharedMindSystem>();
        var objectivesSystem = server.System<ObjectivesSystem>();

        await server.AddDummySession(DummyUsername);
        await server.WaitRunTicks(5);

        var playerSession = playerMan.Sessions.Single();

        Entity<MindComponent>? mindEnt = null;
        await server.WaitPost(() =>
        {
            mindEnt = mindSys.CreateMind(playerSession.UserId);
        });

        Assert.That(mindEnt, Is.Not.Null);
        var mindComp = mindEnt!.Value.Comp;
        Assert.That(mindComp.Objectives, Is.Empty, "Dummy player started with objectives.");

        await pair.WaitCommand($"addobjective {playerSession.Name} {ObjectiveProtoId}");

        Assert.That(mindComp.Objectives, Has.Count.EqualTo(1), "addobjective failed to increase Objectives count.");

        await pair.WaitCommand($"lsobjectives {playerSession.Name}");

        await pair.WaitCommand($"rmobjective {playerSession.Name} 0");

        Assert.That(mindComp.Objectives, Is.Empty, "rmobjective failed to remove objective");

        await pair.CleanReturnAsync();
    }
}
