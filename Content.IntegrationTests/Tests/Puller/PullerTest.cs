using System.Collections.Generic;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Puller;

#nullable enable

[TestFixture]
public sealed class PullerTest : InteractionTest
{
    /// <summary>
    /// Checks that needsHands on PullerComponent is not set on mobs that don't even have hands.
    /// </summary>
    [Test]
    public async Task PullerSanityTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var compFactory = server.ResolveDependency<IComponentFactory>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.TryGetComponent(out PullerComponent? puller, compFactory))
                        continue;

                    if (!puller.NeedsHands)
                        continue;

                    Assert.That(proto.HasComponent<HandsComponent>(compFactory), $"Found puller {proto} with NeedsHand pulling but has no hands?");
                }
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task PullerIsConsideredInteractingTest()
    {
        await SpawnTarget("MobHuman");
        var puller = await SpawnEntity("MobHuman", SEntMan.GetCoordinates(TargetCoords));

        var pullSys = SEntMan.System<PullingSystem>();
        await Server.WaitPost(() => pullSys.TryStartPull(puller, SEntMan.GetEntity(Target.Value)));

        var list = new HashSet<EntityUid>();
        Server.System<SharedInteractionSystem>()
            .GetEntitiesInteractingWithTarget(SEntMan.GetEntity(Target.Value), list);
        Assert.That(list, Is.EquivalentTo([puller]));
    }
}
