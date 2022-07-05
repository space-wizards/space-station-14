using System.Linq;
using Content.Server.Cleanable;
using Content.Server.Decals;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;

namespace Content.Server.Chemistry.TileReactions
{
    [DataDefinition]
    public sealed class CleanTileReaction : ITileReaction
    {
        /// <summary>
        ///     Multiplier used in CleanTileReaction.
        ///     1 (default) means normal consumption rate of the cleaning reagent.
        ///     0 means no consumption of the cleaning reagent, i.e. the reagent is inexhaustible.
        /// </summary>
        [DataField("cleanAmountMultiplier")]
        public float CleanAmountMultiplier { get; private set; } = 1.0f;

        FixedPoint2 ITileReaction.TileReact(TileRef tile, ReagentPrototype reagent, FixedPoint2 reactVolume)
        {
            var entities = EntitySystem.Get<EntityLookupSystem>().GetEntitiesIntersecting(tile).ToArray();
            var amount = FixedPoint2.Zero;
            var entMan = IoCManager.Resolve<IEntityManager>();
            foreach (var entity in entities)
            {
                if (entMan.TryGetComponent(entity, out CleanableComponent? cleanable))
                {
                    var next = amount + (cleanable.CleanAmount * CleanAmountMultiplier);
                    // Nothing left?
                    if (reactVolume < next)
                        break;

                    amount = next;
                    entMan.QueueDeleteEntity(entity);
                }
            }

            var decalSystem = EntitySystem.Get<DecalSystem>();
            foreach (var (uid, _) in decalSystem.GetDecalsInRange(tile.GridUid, tile.GridIndices+new Vector2(0.5f, 0.5f), validDelegate: x => x.Cleanable))
            {
                decalSystem.RemoveDecal(tile.GridUid, uid);
            }

            return amount;
        }
    }
}
