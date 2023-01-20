#nullable enable

using System.Threading.Tasks;
using Content.Server.Climbing.Components;
using Content.Shared.Climbing;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.GameObjects.Components.Movement
{
    [TestFixture]
    [TestOf(typeof(ClimbableComponent))]
    [TestOf(typeof(ClimbingComponent))]
    public sealed class ClimbUnitTest
    {
        private const string Prototypes = @"
- type: entity
  name: HumanDummy
  id: HumanDummy
  components:
  - type: Climbing
  - type: Physics

- type: entity
  name: TableDummy
  id: TableDummy
  components:
  - type: Climbable
  - type: Physics
";

        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            EntityUid human;
            EntityUid table;
            ClimbingComponent climbing;

            await server.WaitAssertion(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                var entityManager = IoCManager.Resolve<IEntityManager>();

                // Spawn the entities
                human = entityManager.SpawnEntity("HumanDummy", MapCoordinates.Nullspace);
                table = entityManager.SpawnEntity("TableDummy", MapCoordinates.Nullspace);

                // Test for climb components existing
                // Players and tables should have these in their prototypes.
                Assert.That(entityManager.TryGetComponent(human, out climbing!), "Human has no climbing");
                Assert.That(entityManager.TryGetComponent(table, out ClimbableComponent? _), "Table has no climbable");

                // TODO ShadowCommander: Implement climbing test
                // // Now let's make the player enter a climbing transitioning state.
                // climbing.IsClimbing = true;
                // EntitySystem.Get<ClimbSystem>().MoveEntityToward(human, table, climbing:climbing);
                // var body = entityManager.GetComponent<PhysicsComponent>(human);
                // // TODO: Check it's climbing
                //
                // // Force the player out of climb state. It should immediately remove the ClimbController.
                // climbing.IsClimbing = false;
            });

            await pairTracker.CleanReturnAsync();
        }
    }
}
