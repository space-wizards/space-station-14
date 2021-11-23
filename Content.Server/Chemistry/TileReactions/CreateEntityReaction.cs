using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.TileReactions;

[DataDefinition]
public class CreateEntityReaction : ITileReaction
{
    [DataField("entity", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Entity = default!;

    [DataField("usage")]
    public FixedPoint2 Usage = FixedPoint2.New(1);

    /// <summary>
    ///     How many of the same entity can fit on one tile?
    /// </summary>
    [DataField("maxOnTile")]
    public int MaxOnTile = 1;

    public FixedPoint2 TileReact(TileRef tile, ReagentPrototype reagent, FixedPoint2 reactVolume)
    {
        if (reactVolume >= Usage)
        {
            // TODO probably pass this in args like reagenteffects do.
            var entMan = IoCManager.Resolve<IEntityManager>();

            int acc = 0;
            foreach (var ent in tile.GetEntitiesInTile())
            {
                if (ent.Prototype != null && ent.Prototype.ID == Entity)
                    acc += 1;

                if (acc >= MaxOnTile)
                    return FixedPoint2.Zero;
            }

            entMan.SpawnEntity(Entity, tile.GridPosition().Offset(new Vector2(0.5f, 0.5f)));
            return Usage;
        }

        return FixedPoint2.Zero;
    }
}
