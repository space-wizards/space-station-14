using System.Linq;
using Content.Server.Cleanable;
using Content.Server.Coordinates.Helpers;
using Content.Server.Decals;
using Content.Server.GameObjects.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Decals;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
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

            var decalSystem = EntitySystem.Get<DecalSystem>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            foreach (var uid in decalSystem.GetDecalsOnTile(tile.GridIndex, tile.GridIndices,
                x => prototypeManager.TryIndex<DecalPrototype>(x.Id, out var decal) && decal.Tags.Contains("crayon")))
            {
                decalSystem.RemoveDecal(tile.GridIndex, uid);
            }

            return amount;
        }
    }
}
