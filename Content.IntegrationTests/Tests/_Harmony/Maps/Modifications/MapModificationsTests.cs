using System.Numerics;
using Content.Server._Harmony.Maps.Modifications;
using Content.Server._Harmony.Maps.Modifications.Systems;
using Content.Server.Station.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Harmony.Maps.Modifications;

[TestFixture]
public sealed class MapModificationsTests
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: TestEntityToAdd

- type: entity
  id: TestEntityToRemove

- type: mapModification
  id: TestAddition
  additions:
  - prototype: TestEntityToAdd
    name: TESTNAME1
    description: TESTDESCRIPTION1
    position: 1,0
    rotation: 90

- type: mapModification
  id: TestRemoval
  removals:
  - !type:EntityPrototypeSelector
    prototype: TestEntityToRemove

- type: mapModification
  id: TestReplacement
  replacements:
  - from:
    - !type:EntityPrototypeSelector
      prototype: TestEntityToRemove
    newPrototype: TestEntityToAdd
    newName: TESTNAME2
    newDescription: TESTDESCRIPTION2
";

    /// <summary>
    /// Checks that map additions correctly add entities.
    /// </summary>
    [Test]
    public async Task TestAddition()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entityManager = server.ResolveDependency<IEntityManager>();
        var prototypeManager = server.ResolveDependency<IPrototypeManager>();

        var mapModificationSystem = entityManager.EntitySysManager.GetEntitySystem<MapModificationSystem>();

        var testMap = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            mapModificationSystem.ApplyMapModification(
                prototypeManager.Index<MapModificationPrototype>("TestAddition"),
                testMap.Grid);

            var entities = entityManager.GetEntities();

            var foundEntity = entities.FirstOrNull(uid =>
                entityManager.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID == "TestEntityToAdd");

            Assert.That(foundEntity, Is.Not.Null, "Entity was not added!");

            var metaData = entityManager.GetComponent<MetaDataComponent>(foundEntity!.Value);
            var transform = entityManager.GetComponent<TransformComponent>(foundEntity!.Value);

            Assert.Multiple(() =>
            {
                Assert.That(metaData.EntityName, Is.EqualTo("TESTNAME1"), "Name was not set correctly!");
                Assert.That(metaData.EntityDescription,
                    Is.EqualTo("TESTDESCRIPTION1"),
                    "Description was not set correctly!");
                Assert.That(transform.LocalPosition, Is.EqualTo(new Vector2(1, 0)), "Position was not set correctly!");
                Assert.That(transform.LocalRotation, Is.EqualTo(new Angle(double.DegreesToRadians(90))), "Rotation was not set correctly!");
            });
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Checks that map modifications will correctly remove an entity.
    /// </summary>
    [Test]
    public async Task TestRemoval()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entityManager = server.ResolveDependency<IEntityManager>();
        var prototypeManager = server.ResolveDependency<IPrototypeManager>();

        var mapModificationSystem = entityManager.EntitySysManager.GetEntitySystem<MapModificationSystem>();

        var testMap = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            entityManager.Spawn("TestEntityToRemove", new MapCoordinates(0, 0, testMap.MapId));

            mapModificationSystem.ApplyMapModification(
                prototypeManager.Index<MapModificationPrototype>("TestRemoval"),
                testMap.Grid);

            var entities = entityManager.GetEntities();

            var foundEntity = entities.FirstOrNull(uid =>
                entityManager.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID == "TestEntityToRemove");

            Assert.That(foundEntity, Is.Null, "Entity was not deleted!");
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Checks that map modifications will correctly replace an entity.
    /// </summary>
    [Test]
    public async Task TestReplacement()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entityManager = server.ResolveDependency<IEntityManager>();
        var prototypeManager = server.ResolveDependency<IPrototypeManager>();

        var mapModificationSystem = entityManager.EntitySysManager.GetEntitySystem<MapModificationSystem>();

        var testMap = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            entityManager.Spawn("TestEntityToRemove", new MapCoordinates(0.5f, 0, testMap.MapId));

            mapModificationSystem.ApplyMapModification(
                prototypeManager.Index<MapModificationPrototype>("TestReplacement"),
                testMap.Grid);

            var entities = entityManager.GetEntities();

            var foundToRemove = entities.FirstOrNull(uid =>
                entityManager.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID == "TestEntityToRemove");
            var foundToAdd = entities.FirstOrNull(uid =>
                entityManager.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID == "TestEntityToAdd");

            Assert.Multiple(() =>
            {
                Assert.That(foundToRemove, Is.Null, "Entity was not deleted!");
                Assert.That(foundToAdd, Is.Not.Null, "Entity was not added!");
            });

            var metaData = entityManager.GetComponent<MetaDataComponent>(foundToAdd!.Value);
            var transform = entityManager.GetComponent<TransformComponent>(foundToAdd!.Value);

            Assert.Multiple(() =>
            {
                Assert.That(metaData.EntityName, Is.EqualTo("TESTNAME2"), "Name was not set correctly!");
                Assert.That(metaData.EntityDescription,
                    Is.EqualTo("TESTDESCRIPTION2"),
                    "Description was not set correctly!");
                Assert.That(transform.LocalPosition, Is.EqualTo(new Vector2(0.5f, 0)), "Position was not set correctly!");
            });
        });

        await pair.CleanReturnAsync();
    }
}
