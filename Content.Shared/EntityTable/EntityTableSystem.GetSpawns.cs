using System.Linq;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityTable;

public sealed partial class EntityTableSystem
{
    /// <summary>
    /// Rolls the odds of this table and returns the <see cref="EntProtoId"/>s selected.
    /// </summary>
    /// <param name="table">The table to roll. <br/> If null, returns an empty enumerable</param>
    /// <param name="rand">The randomizer to use. <br/> Defaults to the instance <see cref="IoCManager"/> provides</param>
    /// <param name="ctx">The context used for evaluating conditions. <br/> Defaults to empty</param>
    /// <returns>The <see cref="EntProtoId"/>s selected</returns>
    public IEnumerable<EntProtoId> GetSpawns(
        EntityTableSelector? table,
        IRobustRandom? rand = null,
        EntityTableContext? ctx = null
    )
    {
        if (table == null)
            return [];

        rand ??= _random;
        ctx ??= new EntityTableContext();
        return table.Accept(
            GetSpawnsVisitor.Instance,
            new GetSpawnsVisitor.Args(EntityManager, ProtoMan, rand, ctx)
        );
    }

    /// <inheritdoc cref="GetSpawns(EntityTableSelector?, IRobustRandom?, EntityTableContext?)"/>
    public IEnumerable<EntProtoId> GetSpawns(
        EntityTablePrototype entTableProto,
        IRobustRandom? rand = null,
        EntityTableContext? ctx = null
    ) => GetSpawns(entTableProto.Table, rand, ctx);
}

/// <summary>
/// The implementation of <see cref="EntityTableSystem.GetSpawns(EntityTableSelector?, IRobustRandom?, EntityTableContext?)"/>.
/// </summary>
sealed file partial class GetSpawnsVisitor : IEntityTableVisitor<
    GetSpawnsVisitor.Args,
    IEnumerable<EntProtoId>>
{
    private GetSpawnsVisitor() { }
    public static readonly GetSpawnsVisitor Instance = new();

    public record struct Args(
        IEntityManager EntMan,
        IPrototypeManager ProtoMan,
        IRobustRandom Rand,
        EntityTableContext Ctx
    );

    /// Handles the common conditions and individual roll chances that every table uses. <paramref name="rollImpl"/> is
    /// called when a single actual roll is needed.
    private static IEnumerable<EntProtoId> Impl(
        EntityTableSelector selector,
        Args args,
        Func<IEnumerable<EntProtoId>> rollImpl
    )
    {
        if (!selector.CheckConditions(args.EntMan, args.ProtoMan, args.Ctx))
            return [];

        return Enumerable.Range(0, selector.Rolls.Get(args.Rand))
            .SelectMany(_ => args.Rand.Prob(selector.Prob) ? rollImpl() : []);
    }

    public IEnumerable<EntProtoId> VisitAllSelector(AllSelector selector, Args args) =>
        Impl(selector, args, () => selector.Children.SelectMany(child => child.Accept(this, args)));

    public IEnumerable<EntProtoId> VisitEntSelector(EntSelector selector, Args args) =>
        Impl(selector, args, () => Enumerable.Repeat(selector.Id, selector.Amount.Get(args.Rand)));

    public IEnumerable<EntProtoId> VisitGroupSelector(GroupSelector selector, Args args)
    {
        var validWeightedChildren = selector.Children
            .Where(child =>
                child.Weight >= float.Epsilon &&
                child.CheckConditions(args.EntMan, args.ProtoMan, args.Ctx)
            )
            .ToDictionary(child => child, child => child.Weight);

        if (validWeightedChildren.Count == 0)
            return [];

        return Impl(
            selector,
            args,
            () => SharedRandomExtensions.Pick(validWeightedChildren, args.Rand).Accept(this, args)
        );
    }

    public IEnumerable<EntProtoId> VisitNestedSelector(NestedSelector selector, Args args) =>
        Impl(selector, args, () => args.ProtoMan.Index(selector.TableId).Table.Accept(this, args));

    public IEnumerable<EntProtoId> VisitNoneSelector(NoneSelector selector, Args args) => [];
}
