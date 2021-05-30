#nullable enable
using Content.Server.GameObjects.Components.Fluids;
using Content.Server.Interfaces;
using Content.Shared.Chemistry;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    [DataDefinition]
    public class WaterVaporReaction : IGasReactionEffect
    {
        [DataField("reagent")] public string? Reagent { get; } = null;

        [DataField("gas")] public int GasId { get; } = 0;

        [DataField("molesPerUnit")] public float MolesPerUnit { get; } = 1;

        [DataField("puddlePrototype")] public string? PuddlePrototype { get; } = "PuddleSmear";

        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, GridTileLookupSystem gridTileLookup)
        {
            // If any of the prototypes is invalid, we do nothing.
            if (string.IsNullOrEmpty(Reagent) || string.IsNullOrEmpty(PuddlePrototype)) return ReactionResult.NoReaction;

            // If we're not reacting on a tile, do nothing.
            if (holder is not TileAtmosphere tile) return ReactionResult.NoReaction;

            // If we don't have enough moles of the specified gas, do nothing.
            if (mixture.GetMoles(GasId) < MolesPerUnit) return ReactionResult.NoReaction;

            // Remove the moles from the mixture...
            mixture.AdjustMoles(GasId, -MolesPerUnit);

            var tileRef = tile.GridIndices.GetTileRef(tile.GridIndex);
            tileRef.SpillAt(new Solution(Reagent, ReagentUnit.New(MolesPerUnit)), PuddlePrototype, sound: false);

            return ReactionResult.Reacting;
        }
    }
}
