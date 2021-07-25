using System;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
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
    public class ExtinguishTileReaction : ITileReaction
    {
        [DataField("coolingTemperature")] private float _coolingTemperature = 2f;

        public ReagentUnit TileReact(TileRef tile, ReagentPrototype reagent, ReagentUnit reactVolume)
        {
            if (reactVolume <= ReagentUnit.Zero || tile.Tile.IsEmpty)
                return ReagentUnit.Zero;

            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            var environment = atmosphereSystem.GetTileMixture(tile.GridIndex, tile.GridIndices, true);

            if (environment == null || !atmosphereSystem.IsHotspotActive(tile.GridIndex, tile.GridIndices))
                return ReagentUnit.Zero;

            environment.Temperature =
                MathF.Max(MathF.Min(environment.Temperature - (_coolingTemperature * 1000f),
                        environment.Temperature / _coolingTemperature), Atmospherics.TCMB);

            atmosphereSystem.React(tile.GridIndex, tile.GridIndices);
            atmosphereSystem.HotspotExtinguish(tile.GridIndex, tile.GridIndices);

            return ReagentUnit.Zero;
        }
    }
}
