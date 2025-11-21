using System.Collections.Generic;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Puller;

#nullable enable

[TestFixture]
public sealed class InteractingEntitiesTest : InteractionTest
{
    private static readonly EntProtoId MobHuman = "MobHuman";

    [Test]
    public async Task PullerIsConsideredInteractingTest()
    {
        await SpawnTarget(MobHuman);
        var puller = await SpawnEntity(MobHuman.ToString(), SEntMan.GetCoordinates(TargetCoords));

        var pullSys = SEntMan.System<PullingSystem>();
        await Server.WaitPost(() => pullSys.TryStartPull(puller, ToServer(Target.Value)));

        var list = new HashSet<EntityUid>();
        Server.System<SharedInteractionSystem>()
            .GetEntitiesInteractingWithTarget(ToServer(Target.Value), list);
        Assert.That(list, Is.EquivalentTo([puller]));
    }
}
