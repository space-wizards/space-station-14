using System.Collections.Generic;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Puller;

#nullable enable

[TestFixture]
public sealed class PullerTest
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
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.EntMan;
        var xformSys = entityManager.System<SharedTransformSystem>();
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var puller = entityManager.SpawnEntity("MobHuman", map.MapCoords);
            var pulled = entityManager.SpawnEntity("MobHuman", map.MapCoords);

            var coords = xformSys.GetWorldPosition(puller);
            xformSys.SetWorldPosition(pulled, coords);

            Assert.Multiple(() =>
            {
                Assert.That(server.System<PullingSystem>().TryStartPull(puller, pulled));

                var list = new HashSet<EntityUid>();
                server.System<SharedInteractionSystem>().GetEntitiesInteractingWithTarget(pulled, list);
                Assert.That(list, Is.EquivalentTo([puller]));
            });
        });

        await pair.CleanReturnAsync();
    }
}
