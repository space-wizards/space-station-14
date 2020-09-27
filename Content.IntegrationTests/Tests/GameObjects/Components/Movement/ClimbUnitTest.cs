#nullable enable

using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.Physics;
using NUnit.Framework;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.GameObjects.Components.Movement
{
    [TestFixture]
    [TestOf(typeof(ClimbableComponent))]
    [TestOf(typeof(ClimbingComponent))]
    public class ClimbUnitTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServerDummyTicker();

            IEntity human;
            IEntity table;
            ClimbableComponent climbable;
            ClimbingComponent climbing;

            server.Assert(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                mapManager.CreateNewMapEntity(MapId.Nullspace);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                // Spawn the entities
                human = entityManager.SpawnEntity("HumanMob_Content", MapCoordinates.Nullspace);
                table = entityManager.SpawnEntity("Table", MapCoordinates.Nullspace);

                // Test for climb components existing
                // Players and tables should have these in their prototypes.
                Assert.That(human.TryGetComponent(out climbing!), "Human has no climbing", Is.True);
                Assert.That(table.TryGetComponent(out climbable!), "Table has no climbable", Is.True);

                // Now let's make the player enter a climbing transitioning state.
                climbing.IsClimbing = true;
                climbing.TryMoveTo(human.Transform.WorldPosition, table.Transform.WorldPosition);
                var body = human.GetComponent<ICollidableComponent>();

                Assert.That(body.HasController<ClimbController>(), "Player has no ClimbController", Is.True);

                // Force the player out of climb state. It should immediately remove the ClimbController.
                climbing.IsClimbing = false;

                Assert.That(!body.HasController<ClimbController>(), "Player wrongly has a ClimbController", Is.True);

            });

            await server.WaitIdleAsync();
        }
    }
}
