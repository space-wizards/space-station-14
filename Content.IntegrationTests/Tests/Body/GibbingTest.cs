using Content.Shared.Body;
using Content.Shared.Gibbing;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Body;

[TestFixture]
[TestOf(typeof(GibbableOrganSystem))]
public sealed class GibletTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: GibbingBody
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

    [Test]
    public async Task GibletCountTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitIdleAsync();

        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var body = entityManager.SpawnEntity("GibbingBody", mapData.GridCoords);
            var gibbing = entityManager.System<GibbingSystem>();
            var giblets = gibbing.Gib(body);

            Assert.That(giblets.Count, Is.EqualTo(3));

            foreach (var giblet in giblets)
            {
                Assert.That(entityManager.HasComponent<GibbableOrganComponent>(giblet), Is.True);
            }
        });

        await pair.CleanReturnAsync();
    }
}
