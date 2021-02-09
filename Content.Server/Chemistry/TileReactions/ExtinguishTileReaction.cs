using System;
using Content.Server.Atmos;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.TileReactions
{
    [UsedImplicitly]
    public class ExtinguishTileReaction : ITileReaction
    {
        private float _coolingTemperature = 2f;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _coolingTemperature, "coolingTemperature", 2f);
        }

        public ReagentUnit TileReact(TileRef tile, ReagentPrototype reagent, ReagentUnit reactVolume)
        {
            if (reactVolume <= ReagentUnit.Zero || tile.Tile.IsEmpty) return ReagentUnit.Zero;
            var tileAtmos = tile.GridIndices.GetTileAtmosphere(tile.GridIndex);
            if (tileAtmos == null || !tileAtmos.Hotspot.Valid) return ReagentUnit.Zero;
            tileAtmos.Air.Temperature =
                MathF.Max(MathF.Min(tileAtmos.Air.Temperature - (_coolingTemperature * 1000f),
                        tileAtmos.Air.Temperature / _coolingTemperature),
                    Atmospherics.TCMB);
            tileAtmos.Air.React(tileAtmos);
            tileAtmos.Hotspot = new Hotspot();
            tileAtmos.UpdateVisuals();
            return ReagentUnit.Zero;
        }
    }
}
