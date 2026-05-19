using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Physics;

public sealed class AnchorPrototypeTest : GameTest
{
    /// <summary>
    /// Asserts that entityprototypes marked as anchored are also static physics bodies.
    /// </summary>
    [Test]
    [Description("Asserts that entityprototypes marked as anchored are also static physics bodies.")]
    [RunOnSide(Side.Server)]
    public async Task TestStaticAnchorPrototypes()
    {
        using (Assert.EnterMultipleScope())
        {
            var xformCompName = SEntMan.ComponentFactory.GetComponentName<TransformComponent>();
            var physicsCompName = SEntMan.ComponentFactory.GetComponentName<PhysicsComponent>();

            foreach (var ent in SProtoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (!ent.TryGetComponent(xformCompName, out TransformComponent xform)
                    || !ent.TryGetComponent(physicsCompName, out PhysicsComponent physics))
                {
                    continue;
                }

                if (!xform.Anchored)
                    return;

                Assert.That(physics.BodyType, Is.EqualTo(BodyType.Static), $"Found entity prototype {ent} marked as anchored but not static for physics.");
            }
        }
    }
}
