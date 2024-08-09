using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Numerics;

namespace Content.Server.Chemistry.TileReactions;

[DataDefinition]
public sealed partial class CreateEntityTileReaction : ITileReaction
{
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Entity = default!;

    [DataField]
    public FixedPoint2 Usage = FixedPoint2.New(1);

    /// <summary>
    ///     How many of the whitelisted entity can fit on one tile?
    /// </summary>
    [DataField]
    public int MaxOnTile = 1;

    /// <summary>
    ///     The whitelist to use when determining what counts as "max entities on a tile".0
    /// </summary>
    [DataField("maxOnTileWhitelist")]
    public EntityWhitelist? Whitelist;

    [DataField]
    public float RandomOffsetMax = 0.0f;

    public FixedPoint2 TileReact(TileRef tile,
        ReagentPrototype reagent,
        FixedPoint2 reactVolume,
        IEntityManager entityManager,
        List<ReagentData>? data)
    {
        if (reactVolume >= Usage)
        {
            if (Whitelist != null)
            {
                int acc = 0;
                foreach (var ent in tile.GetEntitiesInTile())
                {
                    var whitelistSystem = entityManager.System<EntityWhitelistSystem>();
                    if (whitelistSystem.IsWhitelistPass(Whitelist, ent))
                        acc += 1;

                    if (acc >= MaxOnTile)
                        return FixedPoint2.Zero;
                }
            }

            var random = IoCManager.Resolve<IRobustRandom>();
            var xoffs = random.NextFloat(-RandomOffsetMax, RandomOffsetMax);
            var yoffs = random.NextFloat(-RandomOffsetMax, RandomOffsetMax);

            var center = entityManager.System<TurfSystem>().GetTileCenter(tile);
            var pos = center.Offset(new Vector2(xoffs, yoffs));
            entityManager.SpawnEntity(Entity, pos);

            return Usage;
        }

        return FixedPoint2.Zero;
    }
}
