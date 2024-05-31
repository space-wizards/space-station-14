#nullable enable
using System.Numerics;
using Content.Server.Cuffs;
using Content.Shared.Body.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Hands.Components;
using Robust.Server.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.GameObjects.Components.ActionBlocking
{
    [TestFixture]
    [TestOf(typeof(CuffableComponent))]
    [TestOf(typeof(HandcuffComponent))]
    public sealed class HandCuffTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  name: HumanHandcuffDummy
  id: HumanHandcuffDummy
  components:
  - type: Cuffable
  - type: Hands
  - type: Body
    prototype: Human

- type: entity
  name: HandcuffsDummy
  id: HandcuffsDummy
  components:
  - type: Handcuff
";

        [Test]
        public async Task Test()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            EntityUid human;
            EntityUid otherHuman;
            EntityUid cuffs;
            EntityUid secondCuffs;
            CuffableComponent cuffed = default!;
            HandsComponent hands = default!;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var host = server.ResolveDependency<IServerConsoleHost>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                var cuffableSys = entityManager.System<CuffableSystem>();
                var xformSys = entityManager.System<SharedTransformSystem>();

                // Spawn the entities
                human = entityManager.SpawnEntity("HumanHandcuffDummy", coordinates);
                otherHuman = entityManager.SpawnEntity("HumanHandcuffDummy", coordinates);
                cuffs = entityManager.SpawnEntity("HandcuffsDummy", coordinates);
                secondCuffs = entityManager.SpawnEntity("HandcuffsDummy", coordinates);

                var coords = xformSys.GetWorldPosition(otherHuman);
                xformSys.SetWorldPosition(human, coords);

                // Test for components existing
                Assert.Multiple(() =>
                {
                    Assert.That(entityManager.TryGetComponent(human, out cuffed!), $"Human has no {nameof(CuffableComponent)}");
                    Assert.That(entityManager.TryGetComponent(human, out hands!), $"Human has no {nameof(HandsComponent)}");
                    Assert.That(entityManager.TryGetComponent(human, out BodyComponent? _), $"Human has no {nameof(BodyComponent)}");
                    Assert.That(entityManager.TryGetComponent(cuffs, out HandcuffComponent? _), $"Handcuff has no {nameof(HandcuffComponent)}");
                    Assert.That(entityManager.TryGetComponent(secondCuffs, out HandcuffComponent? _), $"Second handcuffs has no {nameof(HandcuffComponent)}");
                });

                // Test to ensure cuffed players register the handcuffs
                cuffableSys.TryAddNewCuffs(human, human, cuffs, cuffed);
                Assert.That(cuffed.CuffedHandCount, Is.GreaterThan(0), "Handcuffing a player did not result in their hands being cuffed");

                // Test to ensure a player with 4 hands will still only have 2 hands cuffed
                AddHand(entityManager.GetNetEntity(human), host);
                AddHand(entityManager.GetNetEntity(human), host);

                Assert.Multiple(() =>
                {
                    Assert.That(cuffed.CuffedHandCount, Is.EqualTo(2));
                    Assert.That(hands.SortedHands, Has.Count.EqualTo(4));
                });

                // Test to give a player with 4 hands 2 sets of cuffs
                cuffableSys.TryAddNewCuffs(human, human, secondCuffs, cuffed);
                Assert.That(cuffed.CuffedHandCount, Is.EqualTo(4), "Player doesn't have correct amount of hands cuffed");
            });

            await pair.CleanReturnAsync();
        }

        private static void AddHand(NetEntity to, IServerConsoleHost host)
        {
            host.ExecuteCommand(null, $"addhand {to}");
        }
    }
}
