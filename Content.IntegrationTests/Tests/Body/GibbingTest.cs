#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
using Content.Shared.Body;
using Content.Shared.Gibbing;

namespace Content.IntegrationTests.Tests.Body;

[TestOf(typeof(GibbableOrganSystem))]
public sealed class GibletTest : GameTest
{
    private const string GibbingBody = "GibbingBody";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {GibbingBody}
  components:
  - type: Body
  - type: EntityTableContainerFill
    containers:
      body_organs: !type:AllSelector
        children:
        - id: Giblet
        - id: Giblet
        - id: Giblet

- type: entity
  id: Giblet
  components:
  - type: Organ
  - type: GibbableOrgan
  - type: Physics
";

    [SidedDependency(Side.Server)] private GibbingSystem _sGibbing = null!;

    [Test]
    [Description("Checks that gibbing a body produces the expected giblets.")]
    public async Task GibletCountTest()
    {
        await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var body = SSpawnAtPosition(GibbingBody, TestMap!.GridCoords);
            var giblets = _sGibbing.Gib(body);

            Assert.That(giblets, Has.Count.EqualTo(3));

            foreach (var giblet in giblets)
            {
                Assert.That(giblet, Has.Comp<GibbableOrganComponent>(Server));
            }
        });
    }
}
