using System.Numerics;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Interaction
{
    [TestFixture]
    [TestOf(typeof(SharedInteractionSystem))]
    public sealed class InRangeUnobstructed
    {
        private const string HumanId = "MobHuman";

        private const float InteractionRange = SharedInteractionSystem.InteractionRange;

        private const float InteractionRangeDivided15 = InteractionRange / 1.5f;

        private static readonly Vector2 InteractionRangeDivided15X = new(InteractionRangeDivided15, 0f);

        private const float InteractionRangeDivided15Times3 = InteractionRangeDivided15 * 3;

        private const float HumanRadius = 0.35f;

        [Test]
        public async Task EntityEntityTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var sEntities = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var conSystem = sEntities.EntitySysManager.GetEntitySystem<SharedContainerSystem>();
            var tSystem = sEntities.EntitySysManager.GetEntitySystem<TransformSystem>();

            EntityUid origin = default;
            EntityUid other = default;
            MapCoordinates mapCoordinates = default;

            var map = await pair.CreateTestMap();

            await server.WaitAssertion(() =>
            {
                var coordinates = map.MapCoords;

                origin = sEntities.SpawnEntity(HumanId, coordinates);
                other = sEntities.SpawnEntity(HumanId, coordinates);
                conSystem.EnsureContainer<Container>(other, "InRangeUnobstructedTestOtherContainer");
                mapCoordinates = tSystem.GetMapCoordinates(other);
            });

            await server.WaitIdleAsync();

            var interactionSys = sEntities.System<SharedInteractionSystem>();
            var xformSys = sEntities.System<SharedTransformSystem>();
            var xform = sEntities.GetComponent<TransformComponent>(origin);

            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    // Entity <-> Entity
                    Assert.That(interactionSys.InRangeUnobstructed(origin, other));
                    Assert.That(interactionSys.InRangeUnobstructed(other, origin));

                    // Entity <-> MapCoordinates
                    Assert.That(interactionSys.InRangeUnobstructed(origin, mapCoordinates));
                    Assert.That(interactionSys.InRangeUnobstructed(mapCoordinates, origin));
                });

                // Move them slightly apart
                xformSys.SetLocalPosition(origin, xform.LocalPosition + InteractionRangeDivided15X, xform);

                Assert.Multiple(() =>
                {
                    // Entity <-> Entity
                    // Entity <-> Entity
                    Assert.That(interactionSys.InRangeUnobstructed(origin, other));
                    Assert.That(interactionSys.InRangeUnobstructed(other, origin));

                    // Entity <-> MapCoordinates
                    Assert.That(interactionSys.InRangeUnobstructed(origin, mapCoordinates));
                    Assert.That(interactionSys.InRangeUnobstructed(mapCoordinates, origin));
                });

                // Move them out of range
                xformSys.SetLocalPosition(origin, xform.LocalPosition + new Vector2(InteractionRangeDivided15 + HumanRadius * 2f, 0f), xform);

                Assert.Multiple(() =>
                {
                    // Entity <-> Entity
                    Assert.That(interactionSys.InRangeUnobstructed(origin, other), Is.False);
                    Assert.That(interactionSys.InRangeUnobstructed(other, origin), Is.False);

                    // Entity <-> MapCoordinates
                    Assert.That(interactionSys.InRangeUnobstructed(origin, mapCoordinates), Is.False);
                    Assert.That(interactionSys.InRangeUnobstructed(mapCoordinates, origin), Is.False);

                    // Checks with increased range

                    // Entity <-> Entity
                    Assert.That(interactionSys.InRangeUnobstructed(origin, other, InteractionRangeDivided15Times3));
                    Assert.That(interactionSys.InRangeUnobstructed(other, origin, InteractionRangeDivided15Times3));

                    // Entity <-> MapCoordinates
                    Assert.That(interactionSys.InRangeUnobstructed(origin, mapCoordinates, InteractionRangeDivided15Times3));
                    Assert.That(interactionSys.InRangeUnobstructed(mapCoordinates, origin, InteractionRangeDivided15Times3));
                });
            });

            await pair.CleanReturnAsync();
        }
    }
}
