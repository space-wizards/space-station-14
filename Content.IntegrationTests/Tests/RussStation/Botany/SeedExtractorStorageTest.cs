using Content.Server.Botany.Components;
using Content.Shared.RussStation.Botany.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.RussStation.Botany;

[TestFixture]
[TestOf(typeof(SeedExtractorStorageComponent))]
public sealed class SeedExtractorStorageTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: SeedStorageTestExtractor
  components:
  - type: SeedExtractorStorage
  - type: ApcPowerReceiver
  - type: ExtensionCableReceiver
  - type: Transform

- type: entity
  id: SeedStorageTestSeed
  components:
  - type: Seed
    seedId: Tomato
  - type: Item
";

    /// <summary>
    /// Verifies that the SeedExtractorStorageComponent is properly registered
    /// and has the expected default container ID.
    /// </summary>
    [Test]
    public async Task ComponentRegistered()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var extractor = entityManager.SpawnEntity("SeedStorageTestExtractor", mapData.GridCoords);

            Assert.That(entityManager.HasComponent<SeedExtractorStorageComponent>(extractor), Is.True);

            var comp = entityManager.GetComponent<SeedExtractorStorageComponent>(extractor);
            Assert.That(comp.SeedContainerId, Is.EqualTo("seed_extractor_seeds"));
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies that seeds can be inserted into the extractor's container directly.
    /// </summary>
    [Test]
    public async Task SeedInsertionIntoContainer()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var containerSystem = server.ResolveDependency<IEntityManager>().System<SharedContainerSystem>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var extractor = entityManager.SpawnEntity("SeedStorageTestExtractor", mapData.GridCoords);
            var seed = entityManager.SpawnEntity("SeedStorageTestSeed", mapData.GridCoords);
            var comp = entityManager.GetComponent<SeedExtractorStorageComponent>(extractor);

            var container = containerSystem.EnsureContainer<Container>(extractor, comp.SeedContainerId);

            Assert.That(containerSystem.Insert(seed, container), Is.True);
            Assert.That(container.ContainedEntities, Has.Count.EqualTo(1));
            Assert.That(container.ContainedEntities[0], Is.EqualTo(seed));
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies that the seed entity has a SeedComponent when spawned from the test prototype.
    /// </summary>
    [Test]
    public async Task SeedEntityHasSeedComponent()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var seed = entityManager.SpawnEntity("SeedStorageTestSeed", mapData.GridCoords);

            Assert.That(entityManager.HasComponent<SeedComponent>(seed), Is.True);
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies that multiple seeds can be stored in the container.
    /// </summary>
    [Test]
    public async Task MultipleSeedsCanBeStored()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var containerSystem = server.ResolveDependency<IEntityManager>().System<SharedContainerSystem>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var extractor = entityManager.SpawnEntity("SeedStorageTestExtractor", mapData.GridCoords);
            var comp = entityManager.GetComponent<SeedExtractorStorageComponent>(extractor);
            var container = containerSystem.EnsureContainer<Container>(extractor, comp.SeedContainerId);

            var seed1 = entityManager.SpawnEntity("SeedStorageTestSeed", mapData.GridCoords);
            var seed2 = entityManager.SpawnEntity("SeedStorageTestSeed", mapData.GridCoords);
            var seed3 = entityManager.SpawnEntity("SeedStorageTestSeed", mapData.GridCoords);

            Assert.That(containerSystem.Insert(seed1, container), Is.True);
            Assert.That(containerSystem.Insert(seed2, container), Is.True);
            Assert.That(containerSystem.Insert(seed3, container), Is.True);
            Assert.That(container.ContainedEntities, Has.Count.EqualTo(3));
        });

        await pair.CleanReturnAsync();
    }
}
