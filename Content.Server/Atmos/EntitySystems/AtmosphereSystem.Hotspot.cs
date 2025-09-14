using Content.Server.Atmos.Components;
using Content.Server.Decals;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Reactions;
using Content.Shared.Database;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        private static readonly ProtoId<SoundCollectionPrototype> DefaultHotspotSounds = "AtmosHotspot";

        [Dependency] private readonly DecalSystem _decalSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private const int HotspotSoundCooldownCycles = 200;

        private int _hotspotSoundCooldown = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier? HotspotSound { get; private set; } = new SoundCollectionSpecifier(DefaultHotspotSounds);

        /// <summary>
        /// Run every tick on every hotspot
        /// </summary>
        private void ProcessHotspot(
            Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
            TileAtmosphere tile)
        {
            var gridAtmosphere = ent.Comp1;
            if (!tile.Hotspot.Valid)
            {
                gridAtmosphere.HotspotTiles.Remove(tile);
                return;
            }

            AddActiveTile(gridAtmosphere, tile);

            if (!tile.Hotspot.SkippedFirstProcess)
            {
                tile.Hotspot.SkippedFirstProcess = true;
                return;
            }

            if(tile.ExcitedGroup != null)
                ExcitedGroupResetCooldowns(tile.ExcitedGroup);

            // If the hotspot is too weak to exist/doesn't have the correct conditions, yeet it for deletion at the end of the tick
            if ((tile.Hotspot.Temperature < Atmospherics.FireMinimumTemperatureToExist) || (tile.Hotspot.Volume <= 1f)
                || tile.Air == null || tile.Air.GetMoles(Gas.Oxygen) < 0.5f || (tile.Air.GetMoles(Gas.Plasma) < 0.5f && tile.Air.GetMoles(Gas.Tritium) < 0.5f) && tile.PuddleSolutionFlammability == 0)
            {
                tile.Hotspot = new Hotspot();
                tile.Hotspot.Type = tile.PuddleSolutionFlammability > 0 ? HotspotType.Puddle : HotspotType.Gas;
                InvalidateVisuals(ent, tile);
                return;
            }

            PerformHotspotExposure(tile);

            tile.Hotspot.Type = tile.PuddleSolutionFlammability > 0 ? HotspotType.Puddle : HotspotType.Gas;

            if (tile.Hotspot.Bypassing || tile.PuddleSolutionFlammability > 0)
            {
                tile.Hotspot.State = 3;

                var gridUid = ent.Owner;
                var tilePos = tile.GridIndices;

                // Get the existing decals on the tile
                var tileDecals = _decalSystem.GetDecalsInRange(gridUid, tilePos);

                // Count the burnt decals on the tile
                var tileBurntDecals = 0;

                foreach (var set in tileDecals)
                {
                    if (Array.IndexOf(_burntDecals, set.Decal.Id) == -1)
                        continue;

                    tileBurntDecals++;

                    if (tileBurntDecals > 4)
                        break;
                }

                // Add a random burned decal to the tile only if there are less than 4 of them
                if (tileBurntDecals < 4)
                    _decalSystem.TryAddDecal(_burntDecals[_random.Next(_burntDecals.Length)], new EntityCoordinates(gridUid, tilePos), out _, cleanable: true);

                if (tile.Air.Temperature > Atmospherics.FireMinimumTemperatureToSpread)
                {
                    var radiatedTemperature = tile.Air.Temperature * Atmospherics.FireSpreadRadiosityScale;
                    foreach (var otherTile in tile.AdjacentTiles)
                    {
                        // TODO ATMOS: This is sus. Suss this out.
                        if (otherTile == null)
                            continue;

                        if(!otherTile.Hotspot.Valid)
                            HotspotExpose(gridAtmosphere, otherTile, radiatedTemperature, Atmospherics.CellVolume/4);
                    }
                }
            }
            else
            {
                tile.Hotspot.State = (byte) (tile.Hotspot.Volume > Atmospherics.CellVolume * 0.4f ? 2 : 1);
            }

            if (tile.Hotspot.Temperature > tile.MaxFireTemperatureSustained)
                tile.MaxFireTemperatureSustained = tile.Hotspot.Temperature;

            if (_hotspotSoundCooldown++ == 0 && HotspotSound != null)
            {
                var coordinates = _mapSystem.ToCenterCoordinates(tile.GridIndex, tile.GridIndices);

                // A few details on the audio parameters for fire.
                // The greater the fire state, the lesser the pitch variation.
                // The greater the fire state, the greater the volume.
                _audio.PlayPvs(HotspotSound, coordinates, HotspotSound.Params.WithVariation(0.15f / tile.Hotspot.State).WithVolume(-5f + 5f * tile.Hotspot.State));
            }

            if (_hotspotSoundCooldown > HotspotSoundCooldownCycles)
                _hotspotSoundCooldown = 0;

            // TODO ATMOS Maybe destroy location here?
        }

        /// <summary>
        /// Run whenever you want to try start a hotspot: run every tick by ignition sources, and also ran on tiles whenever a fire/hotspot is spreading
        /// </summary>
        private void HotspotExpose(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile,
            float exposedTemperature, float exposedVolume, bool soh = false, EntityUid? sparkSourceUid = null)
        {
            if (tile.Air == null)
                return;

            var oxygen = tile.Air.GetMoles(Gas.Oxygen);

            if (oxygen < 0.5f)
                return;

            var plasma = tile.Air.GetMoles(Gas.Plasma);
            var tritium = tile.Air.GetMoles(Gas.Tritium);
            var puddleFlammability = tile.PuddleSolutionFlammability;

            // If a hotspot already exists on this tile, just strengthen it and return early.
            if (tile.Hotspot.Valid)
            {
                if (soh)
                {
                    if (plasma > 0.5f || tritium > 0.5f || puddleFlammability > 0)
                    {
                        if (tile.Hotspot.Temperature < exposedTemperature)
                            tile.Hotspot.Temperature = exposedTemperature;
                        if (tile.Hotspot.Volume < exposedVolume)
                            tile.Hotspot.Volume = exposedVolume;
                    }
                }
                tile.Hotspot.Temperature = AddClampedTemperature(tile.Hotspot.Temperature, 5 * puddleFlammability, (float)(Atmospherics.T0C + 20 * Math.Pow(puddleFlammability, 2)));

                return;
            }

            // If the conditions are right for a hotspot to be created, do so!
            if ((exposedTemperature > Atmospherics.PlasmaMinimumBurnTemperature && (plasma > 0.5f || tritium > 0.5f)) || (puddleFlammability > 0 && exposedTemperature > 573.15 - 50 * puddleFlammability) )
            {
                if (sparkSourceUid.HasValue)
                    _adminLog.Add(LogType.Flammable, LogImpact.High, $"Heat/spark of {ToPrettyString(sparkSourceUid.Value)} caused atmos ignition of gas: {tile.Air.Temperature.ToString():temperature}K - {oxygen}mol Oxygen, {plasma}mol Plasma, {tritium}mol Tritium");

                var temperature = exposedTemperature;
                if(puddleFlammability > 0)
                    temperature = AddClampedTemperature(temperature, 5 * puddleFlammability, (float)(Atmospherics.T0C + 20 * Math.Pow(puddleFlammability, 2)));
                tile.Hotspot = new Hotspot
                {
                    Volume = exposedVolume * 25f,
                    Temperature = temperature,
                    SkippedFirstProcess = tile.CurrentCycle > gridAtmosphere.UpdateCounter,
                    Valid = true,
                    State = 1,
                    Type = puddleFlammability > 0 ? HotspotType.Puddle : HotspotType.Gas
                };

                AddActiveTile(gridAtmosphere, tile);
                gridAtmosphere.HotspotTiles.Add(tile);
            }
        }

        /// <summary>
        /// The actual meat of how a hotspot reacts with the atmos system is done here, called via ProcessHotspot once a tick
        /// </summary>
        private void PerformHotspotExposure(TileAtmosphere tile)
        {
            if (tile.Air == null || !tile.Hotspot.Valid)
                return;

            // A bypassing hotspot does NOT interact with atmos (intended for plasma/trit fires "carrying" the hotspot with them)
            tile.Hotspot.Bypassing = tile.Hotspot.SkippedFirstProcess && tile.Hotspot.Volume > tile.Air.Volume*0.95f && tile.PuddleSolutionFlammability == 0;

            if (tile.Hotspot.Bypassing)
            {
                tile.Hotspot.Volume = tile.Air.ReactionResults[(byte)GasReaction.Fire] * Atmospherics.FireGrowthRate;
                tile.Hotspot.Temperature = tile.Air.Temperature;
            }
            else
            {
                var affected = tile.Air.RemoveVolume(tile.Hotspot.Volume);
                affected.Temperature = MathF.Max(tile.Hotspot.Temperature, Atmospherics.T0C + 50 * tile.PuddleSolutionFlammability);
                React(affected, tile);
                tile.Hotspot.Temperature = affected.Temperature;
                tile.Hotspot.Volume = affected.ReactionResults[(byte)GasReaction.Fire] * Atmospherics.FireGrowthRate;
                Merge(tile.Air, affected);
            }

            var fireEvent = new TileFireEvent(tile.Hotspot.Temperature, tile.Hotspot.Volume);
            _entSet.Clear();
            _lookup.GetLocalEntitiesIntersecting(tile.GridIndex, tile.GridIndices, _entSet, 0f);

            foreach (var entity in _entSet)
            {
                RaiseLocalEvent(entity, ref fireEvent);
            }
        }

        /// <summary>
        /// Used for reagent fires to ensure the temperature doesn't get too far out of control.
        /// </summary>
        private float AddClampedTemperature(float temperature, float kelvinToAdd, float clampTemperature)
        {
            return MathF.Max(temperature, MathF.Min(temperature + kelvinToAdd, clampTemperature));
        }
    }
}
