using System;
using Content.Server.Atmos;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.TileReactions
{
    [UsedImplicitly]
    public class FlammableTileReaction : ITileReaction
    {
        private float _temperatureMultiplier = 1.25f;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _temperatureMultiplier, "temperatureMultiplier", 1.15f);
        }

        public ReagentUnit TileReact(TileRef tile, ReagentPrototype reagent, ReagentUnit reactVolume)
        {
            if (reactVolume <= ReagentUnit.Zero || tile.Tile.IsEmpty) return ReagentUnit.Zero;
            var tileAtmos = tile.GridIndices.GetTileAtmosphere(tile.GridIndex);
            if (tileAtmos == null || !tileAtmos.Hotspot.Valid) return ReagentUnit.Zero;
            tileAtmos.Air.Temperature *= MathF.Max(_temperatureMultiplier * reactVolume.Float(), 1f);
            tileAtmos.Air.React(tileAtmos);
            return reactVolume;
        }
    }
}
