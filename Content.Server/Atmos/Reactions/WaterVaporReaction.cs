using Content.Server.Atmos.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class WaterVaporReaction : IGasReactionEffect
    {
        [DataField("reagent")] public string? Reagent { get; private set; } = null;

        [DataField("gas")] public int GasId { get; private set; } = 0;

        [DataField("molesPerUnit")] public float MolesPerUnit { get; private set; } = 1;

        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
        {
            // If any of the prototypes is invalid, we do nothing.
            if (string.IsNullOrEmpty(Reagent))
                return ReactionResult.NoReaction;

            // If we're not reacting on a tile, do nothing.
            if (holder is not TileAtmosphere tile)
                return ReactionResult.NoReaction;

            // If we don't have enough moles of the specified gas, do nothing.
            if (mixture.GetMoles(GasId) < MolesPerUnit)
                return ReactionResult.NoReaction;

            // Remove the moles from the mixture...
            mixture.AdjustMoles(GasId, -MolesPerUnit);

            var tileRef = atmosphereSystem.GetTileRef(tile);
            atmosphereSystem.Puddle.TrySpillAt(tileRef, new Solution(Reagent, FixedPoint2.New(MolesPerUnit)), out _, sound: false);

            return ReactionResult.Reacting;
        }
    }
}
