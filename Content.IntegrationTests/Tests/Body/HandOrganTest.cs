using System.Collections.Generic;
using System.Linq;
using Content.Shared.Body;
using Content.Shared.Hands.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Body;

[TestFixture]
[TestOf(typeof(HandOrganSystem))]
public sealed class HandOrganTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: TheBody
  components:
  - type: Body
  - type: Hands
  - type: EntityTableContainerFill
    containers:
      body_organs: !type:AllSelector
        children:
        - id: LeftHand
        - id: RightHand

- type: entity
  id: LeftHand
  components:
  - type: Organ
  - type: HandOrgan
    handID: left
    data:
      location: Left

- type: entity
  id: RightHand
  components:
  - type: Organ
  - type: HandOrgan
    handID: right
    data:
      location: Right
";
    [Test]
    public async Task HandInsertionAndRemovalTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitIdleAsync();

        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var container = entityManager.System<SharedContainerSystem>();
            var body = entityManager.SpawnEntity("TheBody", mapData.GridCoords);
            var hands = entityManager.GetComponent<HandsComponent>(body);

            Assert.That(hands.Count, Is.EqualTo(2));

            var handsContainer = container.GetContainer(body, BodyComponent.ContainerID);

            var expectedCount = 2;
            var contained = handsContainer.ContainedEntities.ToList();
            foreach (var hand in contained)
            {
                expectedCount--;
                container.Remove(hand, handsContainer);
                Assert.That(hands.Count, Is.EqualTo(expectedCount));
            }

            var protos = new List<string>() { "LeftHand", "RightHand" };
            foreach (var proto in protos)
            {
                expectedCount++;
                entityManager.SpawnInContainerOrDrop(proto, body, BodyComponent.ContainerID);
                Assert.That(hands.Count, Is.EqualTo(expectedCount));
            }
        });

        await pair.CleanReturnAsync();
    }
}
