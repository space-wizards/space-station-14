using System;
using Content.Server.Atmos;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.Chemistry;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.TileReactions
{
    [UsedImplicitly]
    [DataDefinition]
    public class ExtinguishTileReaction : ITileReaction
    {
        [DataField("coolingTemperature")] private float _coolingTemperature = 2f;

        public ReagentUnit TileReact(TileRef tile, ReagentPrototype reagent, ReagentUnit reactVolume)
        {
            if (reactVolume <= ReagentUnit.Zero || tile.Tile.IsEmpty) return ReagentUnit.Zero;
            var tileAtmos = tile.GridPosition().GetTileAtmosphere();
            if (tileAtmos == null || !tileAtmos.Hotspot.Valid || tileAtmos.Air == null) return ReagentUnit.Zero;
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
