using Content.Server.Decals;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Decals;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Numerics;

namespace Content.Server.Chemistry.TileReactions;

/// <summary>
/// Purges all cleanable decals on a tile.
/// </summary>
[DataDefinition]
public sealed partial class CleanDecalsReaction : ITileReaction
{
    /// <summary>
    /// For every cleaned decal we lose this much reagent.
    /// </summary>
    [DataField]
    public FixedPoint2 CleanCost { get; private set; } = FixedPoint2.New(0.25f);

    public FixedPoint2 TileReact(TileRef tile,
        ReagentPrototype reagent,
        FixedPoint2 reactVolume,
        IEntityManager entityManager)
    {
        if (reactVolume <= CleanCost ||
            !entityManager.TryGetComponent<MapGridComponent>(tile.GridUid, out var grid) ||
            !entityManager.TryGetComponent<DecalGridComponent>(tile.GridUid, out var decalGrid))
        {
            return FixedPoint2.Zero;
        }

        var lookupSystem = entityManager.System<EntityLookupSystem>();
        var decalSystem = entityManager.System<DecalSystem>();
        // Very generous hitbox.
        var decals = decalSystem
            .GetDecalsIntersecting(tile.GridUid, lookupSystem.GetLocalBounds(tile, grid.TileSize).Enlarged(0.5f).Translated(new Vector2(-0.5f, -0.5f)));
        var amount = FixedPoint2.Zero;

        foreach (var decal in decals)
        {
            if (!decal.Decal.Cleanable)
                continue;

            if (amount + CleanCost > reactVolume)
                break;

            decalSystem.RemoveDecal(tile.GridUid, decal.Index, decalGrid);
            amount += CleanCost;
        }

        return amount;
    }
}
