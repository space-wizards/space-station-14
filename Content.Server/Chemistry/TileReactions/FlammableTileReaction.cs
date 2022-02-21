using System;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.TileReactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class FlammableTileReaction : ITileReaction
    {
        [DataField("temperatureMultiplier")] private float _temperatureMultiplier = 1.15f;

        public FixedPoint2 TileReact(TileRef tile, ReagentPrototype reagent, FixedPoint2 reactVolume)
        {
            if (reactVolume <= FixedPoint2.Zero || tile.Tile.IsEmpty)
                return FixedPoint2.Zero;

            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            var environment = atmosphereSystem.GetTileMixture(tile.GridIndex, tile.GridIndices, true);
            if (environment == null || !atmosphereSystem.IsHotspotActive(tile.GridIndex, tile.GridIndices))
                return FixedPoint2.Zero;

            environment.Temperature *= MathF.Max(_temperatureMultiplier * reactVolume.Float(), 1f);
            atmosphereSystem.React(tile.GridIndex, tile.GridIndices);

            return reactVolume;
        }
    }
}
