using System.Linq;
using Content.Server.Cleanable;
using Content.Server.Coordinates.Helpers;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.TileReactions
{
    [DataDefinition]
    public class CleanTileReaction : ITileReaction
    {

        [DataField("cleanAmountMultiplier")]
        public float CleanAmountMultiplier { get; private set; } = 1.0f;

        FixedPoint2 ITileReaction.TileReact(TileRef tile, ReagentPrototype reagent, FixedPoint2 reactVolume)
        {
            var entities = tile.GetEntitiesInTileFast().ToArray();
            var amount = FixedPoint2.Zero;
            foreach (var entity in entities)
            {
                if (IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out CleanableComponent? cleanable))
                {
                    var next = (amount + cleanable.CleanAmount) * CleanAmountMultiplier;
                    // Nothing left?
                    if (reactVolume < next)
                        break;

                    amount = next;
                    IoCManager.Resolve<IEntityManager>().QueueDeleteEntity((EntityUid) entity);
                }
            }

            return amount;
        }
    }
}
