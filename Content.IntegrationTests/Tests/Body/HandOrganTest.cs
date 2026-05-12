#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Body;
using Content.Shared.Hands.Components;
using Robust.Shared.Containers;

namespace Content.IntegrationTests.Tests.Body;

[TestOf(typeof(HandOrganSystem))]
public sealed class HandOrganTest : GameTest
{
    private const string TheBody = "TheBody";
    private const string LeftHand = "LeftHand";
    private const string RightHand = "RightHand";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {TheBody}
  components:
  - type: Body
  - type: Hands
  - type: EntityTableContainerFill
    containers:
      body_organs: !type:AllSelector
        children:
        - id: {LeftHand}
        - id: {RightHand}

- type: entity
  id: {LeftHand}
  components:
  - type: Organ
  - type: HandOrgan
    handID: left
    data:
      location: Left

- type: entity
  id: {RightHand}
  components:
  - type: Organ
  - type: HandOrgan
    handID: right
    data:
      location: Right
";

    [SidedDependency(Side.Server)] private SharedContainerSystem _sContainerSystem = null!;

    [Test]
    public async Task HandInsertionAndRemovalTest()
    {
        await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var body = SSpawnAtPosition(TheBody, TestMap!.GridCoords);
            var hands = SComp<HandsComponent>(body);

            Assert.That(hands, Has.Count.EqualTo(2));

            var handsContainer = _sContainerSystem.GetContainer(body, BodyComponent.ContainerID);

            var expectedCount = 2;
            var contained = handsContainer.ContainedEntities.ToList();
            foreach (var hand in contained)
            {
                expectedCount--;
                _sContainerSystem.Remove(hand, handsContainer);
                Assert.That(hands, Has.Count.EqualTo(expectedCount));
            }

            var protos = new List<string>() { LeftHand, RightHand };
            foreach (var proto in protos)
            {
                expectedCount++;
                SEntMan.SpawnInContainerOrDrop(proto, body, BodyComponent.ContainerID);
                Assert.That(hands, Has.Count.EqualTo(expectedCount));
            }
        });
    }
}
