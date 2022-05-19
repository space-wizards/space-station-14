using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.Chemistry.TileReactions;

[UsedImplicitly]
[DataDefinition]
public sealed class PryTileReaction : ITileReaction
{
    public FixedPoint2 TileReact(TileRef tile, ReagentPrototype reagent, FixedPoint2 reactVolume)
    {
        tile.PryTile();
        return reactVolume;
    }
}
