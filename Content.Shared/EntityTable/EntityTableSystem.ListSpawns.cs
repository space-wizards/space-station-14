using System.Linq;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityTable;

public sealed partial class EntityTableSystem
{
    /// <summary>
    /// Returns the odds of spawning at least one of each entity in this and child tables, as would be returned by
    /// <see cref="EntityTableSystem.GetSpawns(EntityTableSelector?, IRobustRandom?, EntityTableContext?)"/>.
    /// </summary>
    /// <param name="table">The table to get the spawns from. <br/> If null, returns an empty enumerable.</param>
    public IEnumerable<(EntProtoId spawn, float)> ListSpawns(EntityTableSelector? table) =>
        table?.Accept(ListSpawnsVisitor.Instance, new ListSpawnsVisitor.Args(ProtoMan)) ?? [];

    /// <inheritdoc cref="ListSpawns(EntityTableSelector?)"/>
    public IEnumerable<(EntProtoId spawn, float)> ListSpawns(EntityTablePrototype entTableProto) =>
        ListSpawns(entTableProto.Table);
}

/// <summary>
/// The implementation of <see cref="EntityTableSystem.ListSpawns(EntityTableSelector?)"/>.
/// </summary>
sealed file partial class ListSpawnsVisitor : IEntityTableVisitor<
    ListSpawnsVisitor.Args,
    IEnumerable<(EntProtoId spawn, float probability)>
>
{
    // Private constructor enforces usage of `Instance`.
    private ListSpawnsVisitor() { }

    /// <summary>
    /// Ths instance of this visitor. Because the visitor has no state, the same instance can be reused for all cases.
    /// </summary>
    public static readonly ListSpawnsVisitor Instance = new();

    /// <summary>
    /// The arguments for visiting this visitor.
    /// </summary>
    public record struct Args(IPrototypeManager ProtoMan);

    /// <inheritdoc/>
    public IEnumerable<(EntProtoId spawn, float probability)> VisitAllSelector(AllSelector selector, Args args)
    {
        var count = selector.Prob * selector.Rolls.Odds();
        return selector.Children.SelectMany(child =>
            child.Accept(this, args).Select(t => (t.spawn, t.probability * count)));
    }

    /// <inheritdoc/>
    public IEnumerable<(EntProtoId spawn, float probability)> VisitEntSelector(EntSelector selector, Args args)
    {
        yield return (selector.Id, selector.Prob * selector.Rolls.Odds());
    }

    /// <inheritdoc/>
    public IEnumerable<(EntProtoId spawn, float probability)> VisitGroupSelector(
        GroupSelector selector,
        Args args
    )
    {
        var sumOfWeights = selector.Children.Sum(c => c.Weight);
        var odds = sumOfWeights < float.Epsilon ? 0f : selector.Prob * selector.Rolls.Odds() / sumOfWeights;
        return selector.Children.SelectMany(child =>
            child.Accept(this, args).Select(t => (t.spawn, t.probability * child.Weight * odds)));
    }

    /// <inheritdoc/>
    public IEnumerable<(EntProtoId spawn, float probability)> VisitNestedSelector(
        NestedSelector selector,
        Args args
    )
    {
        var count = selector.Prob * selector.Rolls.Odds();
        return args.ProtoMan.Index(selector.TableId)
            .Table.Accept(this, args)
            .Select(t => (t.spawn, t.probability * count));
    }

    /// <inheritdoc/>
    public IEnumerable<(EntProtoId spawn, float probability)> VisitNoneSelector(NoneSelector selector, Args args) => [];
}
