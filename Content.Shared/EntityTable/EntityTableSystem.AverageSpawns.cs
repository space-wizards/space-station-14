using System.Linq;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityTable;

public sealed partial class EntityTableSystem
{
    /// <summary>
    /// Returns the average probability of spawning each entity in this and child tables, as would be returned by
    /// <see cref="EntityTableSystem.GetSpawns(EntityTableSelector?, IRobustRandom?, EntityTableContext?)"/>.
    /// </summary>
    /// <param name="table">The table to get the spawns from. <br/> If null, returns an empty enumerable.</param>
    public IEnumerable<(EntProtoId spawn, float)> AverageSpawns(EntityTableSelector? table) =>
        table?.Accept(AverageSpawnsVisitor.Instance, new AverageSpawnsVisitor.Args(ProtoMan)) ?? [];

    /// <inheritdoc cref="AverageSpawns(EntityTableSelector?)"/>
    public IEnumerable<(EntProtoId spawn, float)> AverageSpawns(EntityTablePrototype entTableProto) =>
        AverageSpawns(entTableProto.Table);
}

/// <summary>
/// The implementation of <see cref="EntityTableSystem.AverageSpawns(EntityTableSelector?)"/>.
/// </summary>
sealed file partial class AverageSpawnsVisitor : IEntityTableVisitor<
    AverageSpawnsVisitor.Args,
    IEnumerable<(EntProtoId spawn, float probability)>
>
{
    private AverageSpawnsVisitor() { }
    public static readonly AverageSpawnsVisitor Instance = new();

    public record struct Args(IPrototypeManager ProtoMan);

    public IEnumerable<(EntProtoId spawn, float probability)> VisitAllSelector(AllSelector selector, Args args)
    {
        var averageRolls = selector.Prob * selector.Rolls.Average();
        return selector.Children.SelectMany(child =>
            child.Accept(this, args).Select(t => (t.spawn, t.probability * averageRolls)));
    }

    public IEnumerable<(EntProtoId spawn, float probability)> VisitEntSelector(EntSelector selector, Args args)
    {
        yield return (selector.Id, selector.Amount.Average() * (selector.Prob * selector.Rolls.Average()));
    }

    public IEnumerable<(EntProtoId spawn, float probability)> VisitGroupSelector(
        GroupSelector selector,
        Args args
    )
    {
        var sumOfWeights = selector.Children.Sum(c => c.Weight);
        var avgRolls = sumOfWeights < float.Epsilon ? 0f : selector.Prob * selector.Rolls.Average() / sumOfWeights;
        return selector.Children.SelectMany(child =>
            child.Accept(this, args).Select(t => (t.spawn, t.probability * child.Weight * avgRolls)));
    }

    public IEnumerable<(EntProtoId spawn, float probability)> VisitNestedSelector(
        NestedSelector selector,
        Args args
    )
    {
        var averageRolls = selector.Prob * selector.Rolls.Average();
        return args.ProtoMan.Index(selector.TableId)
            .Table.Accept(this, args)
            .Select(t => (t.spawn, t.probability * averageRolls));
    }

    public IEnumerable<(EntProtoId spawn, float probability)> VisitNoneSelector(NoneSelector selector, Args args) => [];
}
