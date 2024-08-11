using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Physics;

[TestFixture]
public sealed class AnchorPrototypeTest
{
    /// <summary>
    /// Asserts that entityprototypes marked as anchored are also static physics bodies.
    /// </summary>
    [Test]
    public async Task TestStaticAnchorPrototypes()
    {
        await using var pair = await PoolManager.GetServerClient();

        var protoManager = pair.Server.ResolveDependency<IPrototypeManager>();

        await pair.Server.WaitAssertion(() =>
        {
            foreach (var ent in protoManager.EnumeratePrototypes<EntityPrototype>())
            {
                if (!ent.Components.TryGetComponent("Transform", out var xformComp) ||
                   !ent.Components.TryGetComponent("Physics", out var physicsComp))
                {
                    continue;
                }

                var xform = (TransformComponent)xformComp;
                var physics = (PhysicsComponent)physicsComp;

                if (!xform.Anchored)
                    continue;

                Assert.That(physics.BodyType, Is.EqualTo(BodyType.Static), $"Found entity prototype {ent} marked as anchored but not static for physics.");
            }
        });

        await pair.CleanReturnAsync();
    }
}
