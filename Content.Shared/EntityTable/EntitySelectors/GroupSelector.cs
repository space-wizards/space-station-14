using System.Linq;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// Gets the spawns from one of the child selectors, based on the weight of the children
/// </summary>
public sealed partial class GroupSelector : EntityTableSelectorWithChildrenBase
{
    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(IRobustRandom rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        using var scoped = ScopedConditions(ctx);

        var children = new Dictionary<EntityTableSelector, float>(Children.Count);
        foreach (var child in Children)
        {
            // Don't include invalid groups
            if (!child.CheckConditions(entMan, proto, ctx))
                continue;

            children.Add(child, child.Weight);
        }

        if (children.Count == 0)
            yield break;

        var pick = SharedRandomExtensions.Pick(children, rand);

        foreach (var spawn in pick.GetSpawns(rand, entMan, proto, ctx))
        {
            yield return spawn;
        }
    }

    protected override IEnumerable<(EntProtoId spawn, double)> ListSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        var totalWeight = Children.Sum(x => x.Weight);

        foreach (var child in Children)
        {
            var weightMod = child.Weight / totalWeight;
            foreach (var (ent, prob) in child.ListSpawns(entMan, proto, ctx, weightMod))
            {
                yield return (ent, prob);
            }
        }
    }

    protected override IEnumerable<(EntProtoId spawn, double)> AverageSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        var totalWeight = Children.Sum(x => x.Weight);

        foreach (var child in Children)
        {
            var weightMod = child.Weight / totalWeight;
            foreach (var (ent, prob) in child.AverageSpawns(entMan, proto, ctx, weightMod))
            {
                yield return (ent, prob);
            }
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Group({string.Join(", ", Children.Select(x => $"{x}: {x.Weight}"))})";
    }
}
