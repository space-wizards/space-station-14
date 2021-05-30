using System.Threading.Tasks;
using Content.Client.Utility;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Utility;
using NUnit.Framework;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Interaction
{
    [TestFixture]
    [TestOf(typeof(SharedInteractionSystem))]
    [TestOf(typeof(SharedUnobstructedExtensions))]
    [TestOf(typeof(UnobstructedExtensions))]
    public class InRangeUnobstructed : ContentIntegrationTest
    {
        private const string HumanId = "BaseHumanMob_Content";

        private const float InteractionRange = SharedInteractionSystem.InteractionRange;

        private const float InteractionRangeDivided15 = InteractionRange / 1.5f;

        private readonly (float, float) _interactionRangeDivided15X = (InteractionRangeDivided15, 0f);

        private const float InteractionRangeDivided15Times3 = InteractionRangeDivided15 * 3;

        [Test]
        public async Task EntityEntityTest()
        {
            var server = StartServerDummyTicker();

            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            IEntity origin = null;
            IEntity other = null;
            IContainer container = null;
            IComponent component = null;
            EntityCoordinates entityCoordinates = default;
            MapCoordinates mapCoordinates = default;

            server.Assert(() =>
            {
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                origin = entityManager.SpawnEntity(HumanId, coordinates);
                other = entityManager.SpawnEntity(HumanId, coordinates);
                container = ContainerHelpers.EnsureContainer<Container>(other, "InRangeUnobstructedTestOtherContainer");
                component = other.Transform;
                entityCoordinates = other.Transform.Coordinates;
                mapCoordinates = other.Transform.MapPosition;
            });

            await server.WaitIdleAsync();

            server.Assert(() =>
            {
                // Entity <-> Entity
                Assert.True(origin.InRangeUnobstructed(other));
                Assert.True(other.InRangeUnobstructed(origin));

                // Entity <-> Component
                Assert.True(origin.InRangeUnobstructed(component));
                Assert.True(component.InRangeUnobstructed(origin));

                // Entity <-> Container
                Assert.True(origin.InRangeUnobstructed(container));
                Assert.True(container.InRangeUnobstructed(origin));

                // Entity <-> EntityCoordinates
                Assert.True(origin.InRangeUnobstructed(entityCoordinates));
                Assert.True(entityCoordinates.InRangeUnobstructed(origin));

                // Entity <-> MapCoordinates
                Assert.True(origin.InRangeUnobstructed(mapCoordinates));
                Assert.True(mapCoordinates.InRangeUnobstructed(origin));


                // Move them slightly apart
                origin.Transform.LocalPosition += _interactionRangeDivided15X;

                // Entity <-> Entity
                Assert.True(origin.InRangeUnobstructed(other));
                Assert.True(other.InRangeUnobstructed(origin));

                // Entity <-> Component
                Assert.True(origin.InRangeUnobstructed(component));
                Assert.True(component.InRangeUnobstructed(origin));

                // Entity <-> Container
                Assert.True(origin.InRangeUnobstructed(container));
                Assert.True(container.InRangeUnobstructed(origin));

                // Entity <-> EntityCoordinates
                Assert.True(origin.InRangeUnobstructed(entityCoordinates));
                Assert.True(entityCoordinates.InRangeUnobstructed(origin));

                // Entity <-> MapCoordinates
                Assert.True(origin.InRangeUnobstructed(mapCoordinates));
                Assert.True(mapCoordinates.InRangeUnobstructed(origin));


                // Move them out of range
                origin.Transform.LocalPosition += _interactionRangeDivided15X;

                // Entity <-> Entity
                Assert.False(origin.InRangeUnobstructed(other));
                Assert.False(other.InRangeUnobstructed(origin));

                // Entity <-> Component
                Assert.False(origin.InRangeUnobstructed(component));
                Assert.False(component.InRangeUnobstructed(origin));

                // Entity <-> Container
                Assert.False(origin.InRangeUnobstructed(container));
                Assert.False(container.InRangeUnobstructed(origin));

                // Entity <-> EntityCoordinates
                Assert.False(origin.InRangeUnobstructed(entityCoordinates));
                Assert.False(entityCoordinates.InRangeUnobstructed(origin));

                // Entity <-> MapCoordinates
                Assert.False(origin.InRangeUnobstructed(mapCoordinates));
                Assert.False(mapCoordinates.InRangeUnobstructed(origin));


                // Checks with increased range

                // Entity <-> Entity
                Assert.True(origin.InRangeUnobstructed(other, InteractionRangeDivided15Times3));
                Assert.True(other.InRangeUnobstructed(origin, InteractionRangeDivided15Times3));

                // Entity <-> Component
                Assert.True(origin.InRangeUnobstructed(component, InteractionRangeDivided15Times3));
                Assert.True(component.InRangeUnobstructed(origin, InteractionRangeDivided15Times3));

                // Entity <-> Container
                Assert.True(origin.InRangeUnobstructed(container, InteractionRangeDivided15Times3));
                Assert.True(container.InRangeUnobstructed(origin, InteractionRangeDivided15Times3));

                // Entity <-> EntityCoordinates
                Assert.True(origin.InRangeUnobstructed(entityCoordinates, InteractionRangeDivided15Times3));
                Assert.True(entityCoordinates.InRangeUnobstructed(origin, InteractionRangeDivided15Times3));

                // Entity <-> MapCoordinates
                Assert.True(origin.InRangeUnobstructed(mapCoordinates, InteractionRangeDivided15Times3));
                Assert.True(mapCoordinates.InRangeUnobstructed(origin, InteractionRangeDivided15Times3));
            });

            await server.WaitIdleAsync();
        }
    }
}
