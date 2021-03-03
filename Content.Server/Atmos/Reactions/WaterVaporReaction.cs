#nullable enable
using Content.Server.GameObjects.Components.Fluids;
using Content.Server.Interfaces;
using Content.Shared.Chemistry;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    public class WaterVaporReaction : IGasReactionEffect
    {
        private string? _reagent = null;
        private int _gasId = 0;
        private float _molesPerUnit = 1;
        private string? _puddlePrototype;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _gasId, "gas", 0);
            serializer.DataField(ref _molesPerUnit, "molesPerUnit", 1f);
            serializer.DataField(ref _reagent, "reagent", null);
            serializer.DataField(ref _puddlePrototype, "puddlePrototype", "PuddleSmear");
        }

        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, GridTileLookupSystem gridTileLookup)
        {
            // If any of the prototypes is invalid, we do nothing.
            if (string.IsNullOrEmpty(_reagent) || string.IsNullOrEmpty(_puddlePrototype)) return ReactionResult.NoReaction;

            // If we're not reacting on a tile, do nothing.
            if (holder is not TileAtmosphere tile) return ReactionResult.NoReaction;

            // If we don't have enough moles of the specified gas, do nothing.
            if (mixture.GetMoles(_gasId) < _molesPerUnit) return ReactionResult.NoReaction;

            // Remove the moles from the mixture...
            mixture.AdjustMoles(_gasId, -_molesPerUnit);

            var tileRef = tile.GridIndices.GetTileRef(tile.GridIndex);
            tileRef.SpillAt(new Solution(_reagent, ReagentUnit.New(_molesPerUnit)), _puddlePrototype, sound: false);

            return ReactionResult.Reacting;
        }
    }
}
