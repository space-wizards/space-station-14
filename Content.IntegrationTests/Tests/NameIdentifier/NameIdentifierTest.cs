using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.NameIdentifier;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.NameIdentifier;

[TestOf(typeof(NameIdentifierSystem))]
public sealed class NameIdentifierTest : GameTest
{
    private const string NameTestEnt = "NameTestEnt";
    private const string MaxIds = "5";
    private const int MaxIdsInt = 5;

    private const string NumberTest = "NumberTest";

    private const string ParenTestEnt = "ParenTestEnt";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  name: {NameTestEnt}
  id: {NameTestEnt}
  components:
  - type: NameIdentifier
    group: NumberTest

- type: nameIdentifierGroup
  id: {NumberTest}
  minValue: 1
  maxValue: {MaxIds}

- type: entity
  name: {ParenTestEnt}
  id: {ParenTestEnt}
  components:
  - type: NameIdentifier
    group: GenericNumber
";

    [SidedDependency(Side.Server)] private NameIdentifierSystem _nameModifier = default!;

    [Test]
    public async Task ValidatePoolGeneration()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_nameModifier.CurrentIds.ContainsKey(NumberTest),
                "Failed to load test prototypes.");
            Assert.That(_nameModifier.CurrentIds[NumberTest],
                Has.Count.EqualTo(MaxIdsInt),
                "Failed to load test prototypes.");
        }
    }

    [Test]
    public async Task TakeAndReturn()
    {
        await Server.WaitAssertion(() =>
        {
            // Create an entity that will draw a modifier from the pool
            var single = SEntMan.SpawnEntity(NameTestEnt, MapCoordinates.Nullspace);

            Assert.That(_nameModifier.CurrentIds[NumberTest],
                Has.Count.EqualTo(MaxIdsInt - 1),
                "CurrentIds did not decrease.");

            // Delete the entity, which should return the modifier to the pool
            SDeleteNow(single);

            Assert.That(_nameModifier.CurrentIds[NumberTest],
                Has.Count.EqualTo(MaxIdsInt),
                "CurrentIds did not return to max.");
        });
    }

    [Test]
    public async Task ExhaustAndRefillList()
    {
        var entList = new List<EntityUid>();
        var original = _nameModifier.CurrentIds[NumberTest];

        await Server.WaitAssertion(() =>
        {
            // Spawn as many entities as the pool should hold.
            for (var i = 0; i <= MaxIdsInt; i++)
                entList.Add(SEntMan.SpawnEntity(NameTestEnt, MapCoordinates.Nullspace));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_nameModifier.CurrentIds[NumberTest],
                    Has.Count.EqualTo(0),
                    "CurrentIds failed to empty.");
            }

            // Delete all entities, returning them to the pool.
            foreach (var ent in entList)
                SDeleteNow(ent);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_nameModifier.CurrentIds[NumberTest],
                    Has.Count.EqualTo(MaxIdsInt),
                    "CurrentIds failed to refill.");
                Assert.That(original,
                    Is.EqualTo(_nameModifier.CurrentIds[NumberTest]),
                    "List of values was not returned to starting state.");
            }
        });
    }

    [Test]
    public async Task ValidateDefaultGeneration()
    {
        var entList = new List<EntityUid> { };

        await Server.WaitAssertion(() =>
        {
            // Spawn as many entities as the pool should hold.
            for (var i = 0; i < MaxIdsInt; i++)
                entList.Add(SEntMan.SpawnEntity(NameTestEnt, MapCoordinates.Nullspace));

            var nameList = entList
                .Select(p => SEntMan.GetComponent<MetaDataComponent>(p).EntityName)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(nameList, Is.Unique, "A duplicate name was generated.");
                Assert.That(nameList, Has.All.Contain(NameTestEnt), "The base name was not preserved.");
                Assert.That(nameList, Has.All.Match(@"\d+$"), "Created an invalid name.");
            }
        });
    }

    [Test]
    public async Task ParenthesisAppend()
    {
        await Server.WaitAssertion(() =>
        {
            var single = SEntMan.SpawnEntity(ParenTestEnt, MapCoordinates.Nullspace);
            var name = SEntMan.GetComponent<MetaDataComponent>(single).EntityName;
            Assert.That(name, Does.Match($@"{ParenTestEnt} \(\d+\)$"), "Did not create a valid name.");
        });
    }

    // TODO Test generation when pool is empty
    // TODO Test localization
    // TODO Test prefixing
}
