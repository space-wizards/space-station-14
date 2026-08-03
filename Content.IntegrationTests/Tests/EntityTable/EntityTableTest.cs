using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.EntityTable;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests.EntityTable;

[TestFixture]
[TestOf(typeof(EntityTableSystem))]
public sealed class EntityTableTest : GameTest
{
    [SidedDependency(Side.Server)]
    private readonly EntityTableSystem _sEntityTable = null!;

    [SidedDependency(Side.Server)]
    private readonly IPrototypeManager _sProtoMan = null!;

    [TestPrototypes]
    private const string Prototypes = """
    - type: entity
      id: EntityTableTestEnt1

    - type: entity
      id: EntityTableTestEnt2

    - type: entity
      id: EntityTableTestEntWithCost
      components:
      - type: DynamicRuleCost
        cost: 10

    - type: entityTable
      id: EntityTableTestEntSelector
      table: !type:EntSelector
        id: EntityTableTestEnt1

    - type: entityTable
      id: EntityTableTestEntSelectorAmountRolls
      table: !type:EntSelector
        id: EntityTableTestEnt1
        amount: 3
        rolls: 2

    - type: entityTable
      id: EntityTableTestAllSelector
      table: !type:AllSelector
        children:
        - id: EntityTableTestEnt1
        - !type:NoneSelector
        - id: EntityTableTestEnt2

    - type: entityTable
      id: EntityTableTestNoneSelector
      table: !type:NoneSelector

    - type: entityTable
      id: EntityTableTestNestedTable
      table: !type:GroupSelector
        children:
        - id: EntityTableTestEnt1
          weight: 1
        - id: EntityTableTestEnt2
          weight: 2

    - type: entityTable
      id: EntityTableTestGroupAllFail
      table: !type:GroupSelector
        children:
        - id: EntityTableTestEnt1
          conditions:
          - !type:HasBudgetCondition
            costOverride: 100

    - type: entityTable
      id: EntityTableTestEntSelectorWithCost
      table: !type:EntSelector
        id: EntityTableTestEntWithCost
        conditions:
        - !type:HasBudgetCondition

    - type: entityTable
      id: EntityTableTestEntRequireAll
      table: !type:EntSelector
        id: EntityTableTestEnt1
        requireAll: true
        conditions:
        - !type:HasBudgetCondition
          costOverride: 100
        - !type:HasBudgetCondition
          costOverride: 0

    - type: entityTable
      id: EntityTableTestEntRequireAny
      table: !type:EntSelector
        id: EntityTableTestEnt1
        requireAll: false
        conditions:
        - !type:HasBudgetCondition
          costOverride: 100
        - !type:HasBudgetCondition
          costOverride: 0

    - type: entityTable
      id: EntityTableTestDeepComposition
      table: !type:AllSelector
        children:
        - !type:GroupSelector
          children:
          - id: EntityTableTestEnt1
            weight: 1
            conditions:
            - !type:HasBudgetCondition
              costOverride: 100
          - id: EntityTableTestEnt2
            weight: 2
        - id: EntityTableTestEnt1

    - type: entityTable
      id: EntityTableTestChainTable
      table: !type:NestedSelector
        tableId: EntityTableTestNestedTable
    """;

    [Test]
    public void EntSelector_BasicSingleSpawn()
    {
        var result = Run(Table("EntityTableTestEntSelector"));
        Assert.That(result, Is.EquivalentTo(new []{"EntityTableTestEnt1"}));
    }

    [Test]
    public void EntSelector_AmountAndRollsCompose()
    {
        var result = Run(Table("EntityTableTestEntSelectorAmountRolls"));
        Assert.That(result, Has.Length.EqualTo(6));
        Assert.That(result, Is.All.EqualTo("EntityTableTestEnt1"));
    }

    [Test]
    public void AllSelector_CombinesChildrenInOrder()
    {
        var result = Run(Table("EntityTableTestAllSelector"));
        // NoneSelector contributes nothing, so we get Ent1 then Ent2.
        Assert.That(result, Is.EqualTo(new[] { new EntProtoId("EntityTableTestEnt1"), new EntProtoId("EntityTableTestEnt2") }));
    }

    [Test]
    public void NoneSelector_YieldsNothing()
    {
        var result = Run(Table("EntityTableTestNoneSelector"));
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GroupSelector_WeightedPick_DeterministicWithSeed()
    {
        // EntityTableTestNestedTable is a GroupSelector: Ent1 weight 1, Ent2 weight 2.
        var counts = new Dictionary<string, int>();
        for (var i = 0; i < 100; i++)
        {
            var spawn = Run(Table("EntityTableTestNestedTable"), SeededRand(i)).Single();
            counts[spawn] = counts.GetValueOrDefault(spawn) + 1;
        }

        // Ent2 has weight 2, Ent1 has weight 1, so Ent2 should dominate.
        Assert.That(counts["EntityTableTestEnt2"], Is.GreaterThan(counts["EntityTableTestEnt1"]));
        Assert.Multiple(() =>
        {
            Assert.That(counts["EntityTableTestEnt1"], Is.GreaterThan(20));
            Assert.That(counts["EntityTableTestEnt2"], Is.GreaterThan(40));
        });
    }

    [Test]
    public void GroupSelector_AllChildrenConditionsFail_ReturnsEmpty()
    {
        // No budget in context => condition fails => GroupSelector's child pool is empty.
        var result = Run(Table("EntityTableTestGroupAllFail"), ctx: new EntityTableContext());
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void NestedSelector_ResolvesTransitively()
    {
        // EntityTableTestChainTable is a NestedSelector pointing at EntityTableTestNestedTable.
        var chained = Run(Table("EntityTableTestChainTable"), SeededRand(42));
        Assert.That(chained, Has.Length.EqualTo(1));
        Assert.That(chained[0], Is.AnyOf("EntityTableTestEnt1", "EntityTableTestEnt2"));
    }

    [Test]
    public void HasBudgetCondition_GatesEntSelector()
    {
        // EntityTableTestEntSelectorWithCost reads cost 10 from the DynamicRuleCostComponent.

        // Budget 9 => not enough.
        var poor = Run(Table("EntityTableTestEntSelectorWithCost"), ctx: new EntityTableContext(new() { ["Budget"] = 9f }));
        Assert.That(poor, Is.Empty);

        // Budget 10 => enough.
        var rich = Run(Table("EntityTableTestEntSelectorWithCost"), ctx: new EntityTableContext(new() { ["Budget"] = 10f }));
        Assert.That(rich, Is.EquivalentTo(new[] { new EntProtoId("EntityTableTestEntWithCost") }));
    }

    [Test]
    public void RequireAllConditionSemantics()
    {
        // RequireAll = true, one fails => no spawns.
        var requireAllResult = Run(Table("EntityTableTestEntRequireAll"), ctx: new EntityTableContext(new() { ["Budget"] = 50f }));
        Assert.That(requireAllResult, Is.Empty);

        // RequireAll = false, one passes => spawns.
        var requireAnyResult = Run(Table("EntityTableTestEntRequireAny"), ctx: new EntityTableContext(new() { ["Budget"] = 50f }));
        Assert.That(requireAnyResult, Is.EquivalentTo(new[] { new EntProtoId("EntityTableTestEnt1") }));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void DeepComposition_ComplexTree()
    {
        // Ent1's condition fails (Budget 50 < CostOverride 100), so Group only has Ent2.
        var result = Run(Table("EntityTableTestDeepComposition"), SeededRand(1), new EntityTableContext(new() { ["Budget"] = 50f }));
        Assert.That(result, Is.EqualTo(new[] { new EntProtoId("EntityTableTestEnt2"), new EntProtoId("EntityTableTestEnt1") }));
    }

    private static IRobustRandom SeededRand(int seed)
    {
        var rand = new RobustRandom();
        rand.SetSeed(seed);
        return rand;
    }

    private EntityTablePrototype Table(ProtoId<EntityTablePrototype> id) => _sProtoMan.Index(id);

    private EntProtoId[] Run(EntityTablePrototype proto, IRobustRandom rand = null, EntityTableContext ctx = null)
        => _sEntityTable.GetSpawns(proto, rand, ctx)
                        .ToArray();
}
