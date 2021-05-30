using System;
using Content.Server.Atmos;
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
    public class FlammableTileReaction : ITileReaction
    {
        [DataField("temperatureMultiplier")] private float _temperatureMultiplier = 1.15f;

        public ReagentUnit TileReact(TileRef tile, ReagentPrototype reagent, ReagentUnit reactVolume)
        {
            if (reactVolume <= ReagentUnit.Zero || tile.Tile.IsEmpty) return ReagentUnit.Zero;
            var tileAtmos = tile.GridPosition().GetTileAtmosphere();
            if (tileAtmos?.Air == null || !tileAtmos.Hotspot.Valid) return ReagentUnit.Zero;
            tileAtmos.Air.Temperature *= MathF.Max(_temperatureMultiplier * reactVolume.Float(), 1f);
            tileAtmos.Air.React(tileAtmos);
            return reactVolume;
        }
    }
}
