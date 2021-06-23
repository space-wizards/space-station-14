using System;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
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
            EntitySystem.Get<AtmosphereSystem>().React(tileAtmos.Air, tileAtmos);
            return reactVolume;
        }
    }
}
