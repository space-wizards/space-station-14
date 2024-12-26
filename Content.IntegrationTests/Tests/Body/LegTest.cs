using System.Numerics;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Rotation;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Body
{
    [TestFixture]
    [TestOf(typeof(BodyPartComponent))]
    [TestOf(typeof(BodyComponent))]
    public sealed class LegTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  name: HumanBodyAndAppearanceDummy
  id: HumanBodyAndAppearanceDummy
  components:
  - type: Appearance
  - type: Body
    prototype: Human
  - type: StandingState
";

        [Test]
        public async Task RemoveLegsFallTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            EntityUid human = default!;
            AppearanceComponent appearance = null;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var appearanceSystem = entityManager.System<SharedAppearanceSystem>();
            var xformSystem = entityManager.System<SharedTransformSystem>();

            var map = await pair.CreateTestMap();

            await server.WaitAssertion(() =>
            {
                BodyComponent body = null;

                human = entityManager.SpawnEntity("HumanBodyAndAppearanceDummy",
                    new MapCoordinates(Vector2.Zero, map.MapId));

                Assert.Multiple(() =>
                {
                    Assert.That(entityManager.TryGetComponent(human, out body));
                    Assert.That(entityManager.TryGetComponent(human, out appearance));
                });

                Assert.That(!appearanceSystem.TryGetData(human, RotationVisuals.RotationState, out RotationState _, appearance));

                var bodySystem = entityManager.System<BodySystem>();
                var legs = bodySystem.GetBodyChildrenOfType(human, BodyPartType.Leg, body);

                foreach (var leg in legs)
                {
                    xformSystem.DetachEntity(leg.Id, entityManager.GetComponent<TransformComponent>(leg.Id));
                }
            });

            await server.WaitAssertion(() =>
            {
#pragma warning disable NUnit2045
                // Interdependent assertions.
                Assert.That(appearanceSystem.TryGetData(human, RotationVisuals.RotationState, out RotationState state, appearance));
                Assert.That(state, Is.EqualTo(RotationState.Horizontal));
#pragma warning restore NUnit2045
            });
            await pair.CleanReturnAsync();
        }
    }
}
