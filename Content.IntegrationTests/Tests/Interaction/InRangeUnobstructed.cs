using System.Threading.Tasks;
using Content.Shared.Interaction;
using NUnit.Framework;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Interaction
{
    [TestFixture]
    [TestOf(typeof(SharedInteractionSystem))]
    public sealed class InRangeUnobstructed : ContentIntegrationTest
    {
        private const string HumanId = "MobHumanBase";

        private const float InteractionRange = SharedInteractionSystem.InteractionRange;

        private const float InteractionRangeDivided15 = InteractionRange / 1.5f;

        private readonly (float, float) _interactionRangeDivided15X = (InteractionRangeDivided15, 0f);

        private const float InteractionRangeDivided15Times3 = InteractionRangeDivided15 * 3;

        [Test]
        public async Task EntityEntityTest()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var sEntities = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            EntityUid origin = default;
            EntityUid other = default;
            IContainer container = null;
            IComponent component = null;
            EntityCoordinates entityCoordinates = default;
            MapCoordinates mapCoordinates = default;

            server.Assert(() =>
            {
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                origin = sEntities.SpawnEntity(HumanId, coordinates);
                other = sEntities.SpawnEntity(HumanId, coordinates);
                container = other.EnsureContainer<Container>("InRangeUnobstructedTestOtherContainer");
                component = sEntities.GetComponent<TransformComponent>(other);
                entityCoordinates = sEntities.GetComponent<TransformComponent>(other).Coordinates;
                mapCoordinates = sEntities.GetComponent<TransformComponent>(other).MapPosition;
            });

            await server.WaitIdleAsync();

            var interactionSys = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<SharedInteractionSystem>();

            server.Assert(() =>
            {
                // Entity <-> Entity
                Assert.True(interactionSys.InRangeUnobstructed(origin, other));
                Assert.True(interactionSys.InRangeUnobstructed(other, origin));

                // Entity <-> MapCoordinates
                Assert.True(interactionSys.InRangeUnobstructed(origin, mapCoordinates));
                Assert.True(interactionSys.InRangeUnobstructed(mapCoordinates, origin));

                // Move them slightly apart
                sEntities.GetComponent<TransformComponent>(origin).LocalPosition += _interactionRangeDivided15X;

                // Entity <-> Entity
                // Entity <-> Entity
                Assert.True(interactionSys.InRangeUnobstructed(origin, other));
                Assert.True(interactionSys.InRangeUnobstructed(other, origin));

                // Entity <-> MapCoordinates
                Assert.True(interactionSys.InRangeUnobstructed(origin, mapCoordinates));
                Assert.True(interactionSys.InRangeUnobstructed(mapCoordinates, origin));

                // Move them out of range
                sEntities.GetComponent<TransformComponent>(origin).LocalPosition += _interactionRangeDivided15X;

                // Entity <-> Entity
                Assert.False(interactionSys.InRangeUnobstructed(origin, other));
                Assert.False(interactionSys.InRangeUnobstructed(other, origin));

                // Entity <-> MapCoordinates
                Assert.False(interactionSys.InRangeUnobstructed(origin, mapCoordinates));
                Assert.False(interactionSys.InRangeUnobstructed(mapCoordinates, origin));

                // Checks with increased range

                // Entity <-> Entity
                Assert.True(interactionSys.InRangeUnobstructed(origin, other, InteractionRangeDivided15Times3));
                Assert.True(interactionSys.InRangeUnobstructed(other, origin, InteractionRangeDivided15Times3));

                // Entity <-> MapCoordinates
                Assert.True(interactionSys.InRangeUnobstructed(origin, mapCoordinates, InteractionRangeDivided15Times3));
                Assert.True(interactionSys.InRangeUnobstructed(mapCoordinates, origin, InteractionRangeDivided15Times3));
            });

            await server.WaitIdleAsync();
        }
    }
}
