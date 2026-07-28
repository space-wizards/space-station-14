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
    private const string MaxIds = "5";
    private const int MaxIdsInt = 5;

    private const string NameTest = "NameTest";
    private const string NumberTestGroup = "NumberTestGroup";
    private const string PrefixTestGroup = "PrefixTestGroup";
    private const string PrefixTest = "PrefixTest";
    private const string ParenTestEnt = "ParenTestEnt";
    private const string LocTestEnt = "LocTestEnt";

    [TestPrototypes]
    private const string Prototypes =
        $"""
        - type: nameIdentifierGroup
          id: {NumberTestGroup}
          minValue: 1
          maxValue: {MaxIds}

        - type: entity
          name: {NameTest}
          id: {NameTest}
          components:
          - type: NameIdentifier
            group: {NumberTestGroup}

        - type: nameIdentifierGroup
          parent: {NumberTestGroup}
          id: {PrefixTestGroup}
          prefix: true

        - type: entity
          name: {PrefixTest}
          id: {PrefixTest}
          components:
          - type: NameIdentifier
            group: {PrefixTestGroup}

        - type: entity
          name: {ParenTestEnt}
          id: {ParenTestEnt}
          components:
          - type: NameIdentifier
            group: GenericNumber

        - type: localizedDataset
          id: NameIdentifierTest
          values:
            prefix: name-identifier-test-
            count: 1

        - type: nameIdentifierGroup
          id: Localized
          identifierDataset: NameIdentifierTest

        - type: entity
          name: {LocTestEnt}
          id: {LocTestEnt}
          components:
          - type: NameIdentifier
            group: Localized
        """;

    [SidedDependency(Side.Server)] private NameIdentifierSystem _nameModifier = default!;

    /// <inheritdoc/>
    /// <remarks>
    /// We set <see cref="PoolSettings.Connected"/> false because the client has no unique behavior.
    /// Skipping client sim saves several seconds.
    /// </remarks>
    public override PoolSettings PoolSettings => new ()
    {
        Connected = false,
    };

    [Test]
    [Description("Tests that prototypes are loaded into the identifier system properly.")]
    public async Task ValidatePoolGeneration()
    {
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_nameModifier.CurrentIds.ContainsKey(NumberTestGroup),
                    "Failed to load test prototypes.");
                Assert.That(_nameModifier.CurrentIds[NumberTestGroup],
                    Has.Count.EqualTo(MaxIdsInt),
                    "Failed to load test prototypes.");
            }
        });
    }

    [Test]
    [Description("Tests that IDs are removed from the pool and properly returned when deleted.")]
    public async Task TakeAndReturn()
    {
        await Server.WaitAssertion(() =>
        {
            // Create an entity that will draw a modifier from the pool
            var single = SEntMan.Spawn(NameTest);

            Assert.That(_nameModifier.CurrentIds[NumberTestGroup],
                Has.Count.EqualTo(MaxIdsInt - 1),
                "CurrentIds did not decrease.");

            // Delete the entity, which should return the modifier to the pool
            SDeleteNow(single);

            Assert.That(_nameModifier.CurrentIds[NumberTestGroup],
                Has.Count.EqualTo(MaxIdsInt),
                "CurrentIds did not return to max.");
        });
    }

    [Test]
    [Description("Tests that the list of IDs behaves properly when taken to zero values.")]
    public async Task ExhaustAndRefillList()
    {
        var entList = new List<EntityUid>();
        var original = _nameModifier.CurrentIds[NumberTestGroup];

        await Server.WaitAssertion(() =>
        {
            // Spawn as many entities as the pool should hold.
            for (var i = 0; i <= MaxIdsInt; i++)
                entList.Add(SEntMan.Spawn(NameTest));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_nameModifier.CurrentIds[NumberTestGroup],
                    Has.Count.EqualTo(0),
                    "CurrentIds failed to empty.");
            }

            // Delete all entities, returning them to the pool.
            foreach (var ent in entList)
                SDeleteNow(ent);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_nameModifier.CurrentIds[NumberTestGroup],
                    Has.Count.EqualTo(MaxIdsInt),
                    "CurrentIds failed to refill.");
                Assert.That(original,
                    Is.EqualTo(_nameModifier.CurrentIds[NumberTestGroup]),
                    "List of values was not returned to starting state.");
            }
        });
    }

    [Test]
    [Description("Tests that IDs are not duplicated and correctly formatted.")]
    public async Task ValidateDefaultGeneration()
    {
        var entList = new List<EntityUid>();

        await Server.WaitAssertion(() =>
        {
            // Spawn as many entities as the pool should hold.
            for (var i = 0; i < MaxIdsInt; i++)
                entList.Add(SEntMan.Spawn(NameTest));

            var nameList = entList
                .Select(p => SEntMan.GetComponent<MetaDataComponent>(p).EntityName)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(nameList, Is.Unique, "A duplicate name was generated.");
                Assert.That(nameList, Has.All.Contain(NameTest), "The base name was not preserved.");
                Assert.That(nameList, Has.All.Match(@"\d+$"), "Created an invalid name.");
            }
        });
    }

    [Test]
    [Description("Tests that the core parenthesis wrapped modifier works properly.")]
    public async Task ParenthesisAppend()
    {
        await Server.WaitAssertion(() =>
        {
            var single = SEntMan.Spawn(ParenTestEnt);
            Assert.That(
                SEntMan.GetComponent<MetaDataComponent>(single).EntityName,
                Does.Match($@"{ParenTestEnt} \(\d+\)$"),
                "Did not create a valid parenthesis wrapped name."
                );
        });
    }

    [Test]
    [Description("Tests that nothing is attached to the name when the pool of available names is empty.")]
    public async Task DrawWhenEmpty()
    {
        await Server.WaitAssertion(() =>
        {
            // Spawn as many entities as the pool should hold.
            for (var i = 0; i < MaxIdsInt; i++)
                SEntMan.Spawn(NameTest);

            var emptyDraw = SEntMan.Spawn(NameTest);
            var name = SEntMan.GetComponent<MetaDataComponent>(emptyDraw).EntityName;
            Assert.That(name, Is.EqualTo(NameTest), "Created an invalid name.");
        });
    }

    [Test]
    [Description("Tests that a localized value is properly fetched and attached.")]
    public async Task LocalizedIdentifier()
    {
        await Server.WaitAssertion(() =>
        {
            var single = SEntMan.Spawn(LocTestEnt);
            Assert.That(
                SEntMan.GetComponent<MetaDataComponent>(single).EntityName,
                Is.EqualTo($"{LocTestEnt} TestValue"),
                "Did not create a valid localized name."
                );
        });
    }

    [Test]
    [Description("Tests that the prefix setting properly prepends a value.")]
    public async Task PrefixedIdentifier()
    {
        await Server.WaitAssertion(() =>
        {
            var single = SEntMan.Spawn(PrefixTest);
            Assert.That(
                SEntMan.GetComponent<MetaDataComponent>(single).EntityName,
                Does.Match(@"^\d+"),
                "Did not create a valid name with a prefix."
                );
        });
    }
}
