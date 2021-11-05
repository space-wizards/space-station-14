using System.Linq;
using Content.Server.Cleanable;
using Content.Server.Coordinates.Helpers;
using Content.Server.GameObjects.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.TileReactions
{
    [DataDefinition]
    public class CleanTileReaction : ITileReaction
    {
        FixedPoint2 ITileReaction.TileReact(TileRef tile, ReagentPrototype reagent, FixedPoint2 reactVolume)
        {
            var entities = tile.GetEntitiesInTileFast().ToArray();
            var amount = FixedPoint2.Zero;
            foreach (var entity in entities)
            {
                if (entity.TryGetComponent(out CleanableComponent? cleanable))
                {
                    var next = amount + cleanable.CleanAmount;
                    // Nothing left?
                    if (reactVolume < next)
                        break;

                    amount = next;
                    entity.QueueDelete();
                }
            }

            return amount;
        }
    }
}
