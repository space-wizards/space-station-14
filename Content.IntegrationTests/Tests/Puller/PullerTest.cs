using Content.Shared.Hands.Components;
using Content.Shared.Prototypes;
using Content.Shared.Pulling.Components;
using Robust.Shared.GameObjects;
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
                    if (!proto.TryGetComponent(out SharedPullerComponent? puller))
                        continue;

                    if (!puller.NeedsHands)
                        continue;

                    Assert.That(proto.HasComponent<HandsComponent>(compFactory), $"Found puller {proto} with NeedsHand pulling but has no hands?");
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}
