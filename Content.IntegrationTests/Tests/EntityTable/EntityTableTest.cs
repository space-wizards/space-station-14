using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.Conditions;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests.EntityTable;

[TestFixture]
[TestOf(typeof(EntityTableSystem))]
public sealed class EntityTableTest : GameTest
{
    [SidedDependency(Side.Server)]
    private readonly EntityTableSystem _sEntityTable = null!;

    private const string EntProto1 = "EntityTableTestEnt1";
    private const string EntProto2 = "EntityTableTestEnt2";
    private const string EntProtoWithCost = "EntityTableTestEntWithCost";

    [TestPrototypes]
    private const string Prototypes =
        $"""
         - type: entity
           id: {EntProto1}
         
         - type: entity
           id: {EntProto2}
         
         - type: entity
           id: {EntProtoWithCost}
           components:
           - type: DynamicRuleCost
             cost: 10
         
         - type: entityTable
           id: EntityTableTestEntSelector
           table: !type:EntSelector
             id: {EntProto1}
         
         - type: entityTable
           id: EntityTableTestEntSelectorAmountRolls
           table: !type:EntSelector
             id: {EntProto1}
             amount: 3
             rolls: 2
         
         - type: entityTable
           id: EntityTableTestAllSelector
           table: !type:AllSelector
             children:
             - id: {EntProto1}
             - !type:NoneSelector
             - id: {EntProto2}
         
         - type: entityTable
           id: EntityTableTestNoneSelector
           table: !type:NoneSelector
         
         - type: entityTable
           id: EntityTableTestNestedTable
           table: !type:GroupSelector
             children:
             - id: {EntProto1}
               weight: 1
             - id: {EntProto2}
               weight: 2
         
         - type: entityTable
           id: EntityTableTestGroupAllFail
           table: !type:GroupSelector
             children:
             - id: {EntProto1}
               conditions:
               - !type:HasBudgetCondition
                 costOverride: 100
         
         - type: entityTable
           id: EntityTableTestEntSelectorWithCost
           table: !type:EntSelector
             id: {EntProtoWithCost}
             conditions:
             - !type:HasBudgetCondition
         
         - type: entityTable
           id: EntityTableTestEntRequireAll
           table: !type:EntSelector
             id: {EntProto1}
             requireAll: true
             conditions:
             - !type:HasBudgetCondition
               costOverride: 100
             - !type:HasBudgetCondition
               costOverride: 0
         
         - type: entityTable
           id: EntityTableTestEntRequireAny
           table: !type:EntSelector
             id: {EntProto1}
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
               - id: {EntProto1}
                 weight: 1
                 conditions:
                 - !type:HasBudgetCondition
                   costOverride: 100
               - id: {EntProto2}
                 weight: 2
             - id: {EntProto1}
         
         - type: entityTable
           id: EntityTableTestChainTable
           table: !type:NestedSelector
             tableId: EntityTableTestNestedTable
         
         - type: entityTable
           id: EntityTableTestNotRepeating
           table: !type:EntSelector
             id: {EntProto1}
             conditions:
             - !type:ExcludeEntitiesFromContextCondition
         
         - type: entityTable
           id: EntityTableTestAllNotRepeating
           table: !type:AllSelector
             conditionsForChildren:
             - !type:ExcludeEntitiesFromContextCondition
             children:
             - id: {EntProto1}
             - id: {EntProto2}
         
         - type: entityTable
           id: EntityTableTestChainNotRepeating
           table: !type:NestedSelector
             tableId: EntityTableTestNestedTable
             conditionsForChildren:
             - !type:ExcludeEntitiesFromContextCondition
         
         - type: entityTable
           id: EntityTableTestNestedTableWithCost
           table: !type:EntSelector
             id: {EntProtoWithCost}
             conditions:
             - !type:HasBudgetCondition
         
         - type: entityTable
           id: EntityTableTestChainTableWithCost
           table: !type:NestedSelector
             tableId: EntityTableTestNestedTableWithCost
         
         - type: entityTable
           id: EntityTableTestGroupWithCostlyNested
           table: !type:GroupSelector
             children:
             - !type:NestedSelector
               tableId: EntityTableTestNestedTableWithCost
               weight: 1
             - id: {EntProto2}
               weight: 1
               
         - type: entityTable
           id: EntityTableTestLocalizedChildConditions
           table: !type:AllSelector
             children:
             - !type:AllSelector
               conditionsForChildren:
               - !type:ExcludeEntitiesFromContextCondition
               children:
                - id: {EntProto1}
                - id: {EntProto2}
             - id: {EntProto2}
         """;

    [Test]
    [RunOnSide(Side.Server)]
    public void EntSelector_BasicSingleSpawn()
    {
        var result = Run(Table("EntityTableTestEntSelector"));
        Assert.That(result, Is.EquivalentTo(new [] { EntProto1 }));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void EntSelector_AmountAndRollsCompose()
    {
        var result = Run(Table("EntityTableTestEntSelectorAmountRolls"));
        Assert.That(result, Has.Length.EqualTo(6));
        Assert.That(result, Is.All.EqualTo(EntProto1));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void AllSelector_CombinesChildrenInOrder()
    {
        var result = Run(Table("EntityTableTestAllSelector"));
        // NoneSelector contributes nothing, so we get Ent1 then Ent2.
        Assert.That(result, Is.EqualTo(new[] { new EntProtoId(EntProto1), new EntProtoId(EntProto2) }));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void NoneSelector_YieldsNothing()
    {
        var result = Run(Table("EntityTableTestNoneSelector"));
        Assert.That(result, Is.Empty);
    }

    [Test]
    [RunOnSide(Side.Server)]
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
        Assert.That(counts[EntProto2], Is.GreaterThan(counts[EntProto1]));
        Assert.Multiple(() =>
        {
            Assert.That(counts[EntProto1], Is.GreaterThan(20));
            Assert.That(counts[EntProto2], Is.GreaterThan(40));
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void GroupSelector_AllChildrenConditionsFail_ReturnsEmpty()
    {
        // No budget in context => condition fails => GroupSelector's child pool is empty.
        var result = Run(Table("EntityTableTestGroupAllFail"), ctx: new EntityTableContext());
        Assert.That(result, Is.Empty);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void NestedSelector_ResolvesTransitively()
    {
        // EntityTableTestChainTable is a NestedSelector pointing at EntityTableTestNestedTable.
        var chained = Run(Table("EntityTableTestChainTable"), SeededRand(42));
        Assert.That(chained, Has.Length.EqualTo(1));
        Assert.That(chained[0], Is.AnyOf(EntProto1, EntProto2));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void HasBudgetCondition_GatesEntSelector()
    {
        // EntityTableTestEntSelectorWithCost reads cost 10 from the DynamicRuleCostComponent.

        // Budget 9 => not enough.
        var poor = Run(Table("EntityTableTestEntSelectorWithCost"), ctx: new EntityTableContext(new() { ["Budget"] = 9f }));
        Assert.That(poor, Is.Empty);

        // Budget 10 => enough.
        var rich = Run(Table("EntityTableTestEntSelectorWithCost"), ctx: new EntityTableContext(new() { ["Budget"] = 10f }));
        Assert.That(rich, Is.EquivalentTo(new[] { new EntProtoId(EntProtoWithCost) }));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void RequireAllConditionSemantics()
    {
        // RequireAll = true, one fails => no spawns.
        var requireAllResult = Run(Table("EntityTableTestEntRequireAll"), ctx: new EntityTableContext(new() { ["Budget"] = 50f }));
        Assert.That(requireAllResult, Is.Empty);

        // RequireAll = false, one passes => spawns.
        var requireAnyResult = Run(Table("EntityTableTestEntRequireAny"), ctx: new EntityTableContext(new() { ["Budget"] = 50f }));
        Assert.That(requireAnyResult, Is.EquivalentTo(new[] { new EntProtoId(EntProto1) }));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void DeepComposition_ComplexTree()
    {
        // Ent1's condition fails (Budget 50 < CostOverride 100), so Group only has Ent2.
        var result = Run(Table("EntityTableTestDeepComposition"), SeededRand(1), new EntityTableContext(new() { ["Budget"] = 50f }));
        Assert.That(result, Is.EqualTo(new[] { new EntProtoId(EntProto2), new EntProtoId(EntProto1) }));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ExcludeEntitiesFromContextCondition_GatesEntSelector()
    {
        // No UsedSpawns tracking in context => condition passes and the ent spawns.
        var noTracking = Run(Table("EntityTableTestNotRepeating"), ctx: new EntityTableContext());
        Assert.That(noTracking, Is.EquivalentTo(new[] { new EntProtoId(EntProto1) }));

        // EntProto1 already recorded as spawned => blocked.
        var used = new HashSet<EntProtoId> { new(EntProto1) };
        var blocked = Run(Table("EntityTableTestNotRepeating"),
            ctx: new EntityTableContext(new() { [ExcludeEntitiesFromContextCondition.EntitiesToExclude] = used }));
        Assert.That(blocked, Is.Empty);

        // Tracking enabled, but EntProto1 has not been spawned yet => allowed.
        var allowed = Run(Table("EntityTableTestNotRepeating"),
            ctx: new EntityTableContext(new() { [ExcludeEntitiesFromContextCondition.EntitiesToExclude] = new HashSet<EntProtoId>() }));
        Assert.That(allowed, Is.EquivalentTo([new EntProtoId(EntProto1)]));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void AllSelector_ConditionsForChildren_ApplyToChildren()
    {
        var used = new HashSet<EntProtoId> { new(EntProto1) };
        var result = Run(Table("EntityTableTestAllNotRepeating"),
            ctx: new EntityTableContext(new() { [ExcludeEntitiesFromContextCondition.EntitiesToExclude] = used }));
        Assert.That(result, Is.EqualTo(new[] { new EntProtoId(EntProto2) }));
    }

    /// <summary>
    /// EntityTableTestChainNotRepeating forwards the condition into EntityTableTestNestedTable.
    /// EntProto1 already used => excluded from the GroupSelector pool, EntProto2 is picked.
    /// </summary>
    [Test]
    [RunOnSide(Side.Server)]
    public void NestedSelector_ConditionsForChildren_ApplyToNestedChildren()
    {
        var used = new HashSet<EntProtoId> { new(EntProto1) };
        var result = Run(Table("EntityTableTestChainNotRepeating"), SeededRand(1),
            new EntityTableContext(new() { [ExcludeEntitiesFromContextCondition.EntitiesToExclude] = used }));
        Assert.That(result, Is.EqualTo(new[] { new EntProtoId(EntProto2) }));
    }

    /// <summary>
    /// EntityTableTestChainTableWithCost points at a nested table whose EntSelector is gated by
    /// HasBudgetCondition (cost 10). The NestedSelector itself has no conditions, but the nested
    /// table's conditions still gate it.
    /// </summary>
    [Test]
    [RunOnSide(Side.Server)]
    public void NestedSelector_CheckConditions_IncludesNestedTable()
    {
        var poor = Run(Table("EntityTableTestChainTableWithCost"), ctx: new EntityTableContext(new() { ["Budget"] = 9f }));
        Assert.That(poor, Is.Empty);

        var rich = Run(Table("EntityTableTestChainTableWithCost"), ctx: new EntityTableContext(new() { ["Budget"] = 10f }));
        Assert.That(rich, Is.EquivalentTo(new[] { new EntProtoId(EntProtoWithCost) }));
    }

    /// <summary>
    /// The group contains a NestedSelector (cost 10) and EntProto2. With a budget of 9 the nested
    /// selector fails CheckConditions and is excluded from the group's pool, so only EntProto2 can be picked.
    /// </summary>
    [Test]
    [RunOnSide(Side.Server)]
    public void GroupSelector_ExcludesNestedSelectorWithFailingConditions()
    {
        var result = Run(Table("EntityTableTestGroupWithCostlyNested"), SeededRand(1), new EntityTableContext(new() { ["Budget"] = 9f }));
        Assert.That(result, Is.EqualTo(new[] { new EntProtoId(EntProto2) }));
    }

    /// <summary>
    /// Demonstrates how injecting ExcludeEntitiesFromContextCondition through the context's
    /// AdditionalConditionsKey can change the behavior of an otherwise unconstrained table.
    /// </summary>
    [Test]
    [RunOnSide(Side.Server)]
    public void AdditionalConditions_FromContext_ChangeBehavior()
    {
        var used = new HashSet<EntProtoId> { new(EntProto1) };

        // Without the injected condition, the UsedSpawns tracking alone has no effect.
        var unconstrained = Run(Table("EntityTableTestEntSelector"),
            ctx: new EntityTableContext(new() { [ExcludeEntitiesFromContextCondition.EntitiesToExclude] = used }));
        Assert.That(unconstrained, Is.EquivalentTo([new EntProtoId(EntProto1)]));

        // Injecting ExcludeEntitiesFromContextCondition gates the selector: EntProto1 is already used => blocked.
        var ctx = new EntityTableContext(new() { [ExcludeEntitiesFromContextCondition.EntitiesToExclude] = used });
        ctx.SetData(EntityTableSelector.AdditionalConditionsKey,
            new List<EntityTableCondition> { new ExcludeEntitiesFromContextCondition() });
        var blocked = Run(Table("EntityTableTestEntSelector"), ctx: ctx);
        Assert.That(blocked, Is.Empty);

        // With the condition injected but EntProto1 not yet used, the spawn is allowed.
        var fresh = new EntityTableContext(new() { [ExcludeEntitiesFromContextCondition.EntitiesToExclude] = new HashSet<EntProtoId>() });
        fresh.SetData(EntityTableSelector.AdditionalConditionsKey,
            new List<EntityTableCondition> { new ExcludeEntitiesFromContextCondition() });
        var allowed = Run(Table("EntityTableTestEntSelector"), ctx: fresh);
        Assert.That(allowed, Is.EquivalentTo([new EntProtoId(EntProto1)]));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void AdditionalConditions_RemainLocal()
    {
        var used = new HashSet<EntProtoId> { new(EntProto2) };
        var ctx = new EntityTableContext(new() { [ExcludeEntitiesFromContextCondition.EntitiesToExclude] = used });

        var result = Run(Table("EntityTableTestLocalizedChildConditions"), ctx: ctx);

        ctx.TryGetData<object>(EntityTableSelector.AdditionalConditionsKey, out var empty);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(empty, Is.Null);
            Assert.That(result, Is.EquivalentTo([new EntProtoId(EntProto1), new EntProtoId(EntProto2)]));
        }
    }

    private static IRobustRandom SeededRand(int seed)
    {
        var rand = new RobustRandom();
        rand.SetSeed(seed);
        return rand;
    }

    private EntityTablePrototype Table(ProtoId<EntityTablePrototype> id) => SProtoMan.Index(id);

    private EntProtoId[] Run(EntityTablePrototype proto, IRobustRandom rand = null, EntityTableContext ctx = null)
        => _sEntityTable.GetSpawns(proto, rand, ctx)
                        .ToArray();
}
