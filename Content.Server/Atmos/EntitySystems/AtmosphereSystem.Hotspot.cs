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

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    /*
     Handles Hotspots, which are gas-based tile fires that slowly grow and spread
     to adjacent tiles if conditions are met.

     You can think of a hotspot as a small flame on a tile that
     grows by consuming a fuel and oxidizer from the tile's air,
     with a certain volume and temperature.

     This volume grows bigger and bigger as the fire continues,
     until it effectively engulfs the entire tile, at which point
     it starts spreading to adjacent tiles by radiating heat.
     */

    /// <summary>
    /// Collection of hotspot sounds to play.
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultHotspotSounds = "AtmosHotspot";

    [Dependency] private readonly DecalSystem _decalSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Number of cycles the hotspot system must process before it can play another sound
    /// on a hotspot.
    /// </summary>
    private const int HotspotSoundCooldownCycles = 200;

    /// <summary>
    /// Cooldown counter for hotspot sounds.
    /// </summary>
    private int _hotspotSoundCooldown = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? HotspotSound = new SoundCollectionSpecifier(DefaultHotspotSounds);

    /// <summary>
    /// Processes a hotspot on a <see cref="TileAtmosphere"/>.
    /// </summary>
    /// <param name="ent">The grid entity that belongs to the tile to process.</param>
    /// <param name="tile">The <see cref="TileAtmosphere"/> to process.</param>
    private void ProcessHotspot(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
        TileAtmosphere tile)
    {
        var gridAtmosphere = ent.Comp1;

        // Hotspots that have fizzled out are assigned a new Hotspot struct
        // with Valid set to false, so we can just check that here in
        // one central place instead of manually removing it everywhere.
        if (!tile.Hotspot.Valid)
        {
            gridAtmosphere.HotspotTiles.Remove(tile);
            return;
        }

        AddActiveTile(gridAtmosphere, tile);

        // Prevent the hotspot from processing on the same cycle it was created (???)
        // TODO ATMOS: Is this even necessary anymore? The queue is kept per processing stage
        // and is not updated until tne next cycle, so the condition of a hotspot being created
        // and processed in the same cycle is impossible.
        if (!tile.Hotspot.SkippedFirstProcess)
        {
            tile.Hotspot.SkippedFirstProcess = true;
            return;
        }

        if (tile.ExcitedGroup != null)
            ExcitedGroupResetCooldowns(tile.ExcitedGroup);

        if (tile.Hotspot.Temperature < Atmospherics.FireMinimumTemperatureToExist ||
            tile.Hotspot.Volume <= 1f ||
            tile.Air == null ||
            tile.Air.GetMoles(Gas.Oxygen) < 0.5f ||
            tile.Air.GetMoles(Gas.Plasma) < 0.5f && tile.Air.GetMoles(Gas.Tritium) < 0.5f)
        {
            tile.Hotspot = new Hotspot();
            InvalidateVisuals(ent, tile);
            return;
        }

        PerformHotspotExposure(tile);

        // This tile has now turned into a full-blown tile-fire.
        // Start applying fire effects and spreading to adjacent tiles.
        if (tile.Hotspot.Bypassing)
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
            {
                _decalSystem.TryAddDecal(_burntDecals[_random.Next(_burntDecals.Length)],
                    new EntityCoordinates(gridUid, tilePos),
                    out _,
                    cleanable: true);
            }

            if (tile.Air.Temperature > Atmospherics.FireMinimumTemperatureToSpread)
            {
                var radiatedTemperature = tile.Air.Temperature * Atmospherics.FireSpreadRadiosityScale;
                foreach (var otherTile in tile.AdjacentTiles)
                {
                    // TODO ATMOS: This is sus. Suss this out.
                    // Spread this fire to other tiles by exposing them to a hotspot if air can flow there.
                    // Unsure as to why this is sus.
                    if (otherTile == null)
                        continue;

                    if (!otherTile.Hotspot.Valid)
                        HotspotExpose(gridAtmosphere, otherTile, radiatedTemperature, Atmospherics.CellVolume / 4);
                }
            }
        }
        else
        {
            // Little baby fire. Set the sprite state based on the current size of the fire.
            tile.Hotspot.State = (byte)(tile.Hotspot.Volume > Atmospherics.CellVolume * 0.4f ? 2 : 1);
        }

        if (tile.Hotspot.Temperature > tile.MaxFireTemperatureSustained)
            tile.MaxFireTemperatureSustained = tile.Hotspot.Temperature;

        if (_hotspotSoundCooldown++ == 0 && HotspotSound != null)
        {
            var coordinates = _mapSystem.ToCenterCoordinates(tile.GridIndex, tile.GridIndices);

            // A few details on the audio parameters for fire.
            // The greater the fire state, the lesser the pitch variation.
            // The greater the fire state, the greater the volume.
            _audio.PlayPvs(HotspotSound,
                coordinates,
                HotspotSound.Params.WithVariation(0.15f / tile.Hotspot.State)
                    .WithVolume(-5f + 5f * tile.Hotspot.State));
        }

        if (_hotspotSoundCooldown > HotspotSoundCooldownCycles)
            _hotspotSoundCooldown = 0;

        // TODO ATMOS Maybe destroy location here?
    }

    /// <summary>
    /// Exposes a tile to a hotspot of given temperature and volume, igniting it if conditions are met.
    /// </summary>
    /// <param name="gridAtmosphere">The <see cref="GridAtmosphereComponent"/> of the grid the tile is on.</param>
    /// <param name="tile">The <see cref="TileAtmosphere"/> to expose.</param>
    /// <param name="exposedTemperature">The temperature of the hotspot to expose.
    /// You can think of this as exposing a temperature of a flame.</param>
    /// <param name="exposedVolume">The volume of the hotspot to expose.
    /// You can think of this as how big the flame is initially.
    /// Bigger flames will ramp a fire faster.</param>
    /// <param name="soh">Whether to "boost" a fire that's currently on the tile already.
    /// Does nothing if the tile isn't already a hotspot.
    /// This clamps the temperature and volume of the hotspot to the maximum
    /// of the provided parameters and whatever's on the tile.</param>
    /// <param name="sparkSourceUid">Entity that started the exposure for admin logging.</param>
    private void HotspotExpose(GridAtmosphereComponent gridAtmosphere,
        TileAtmosphere tile,
        float exposedTemperature,
        float exposedVolume,
        bool soh = false,
        EntityUid? sparkSourceUid = null)
    {
        if (tile.Air == null)
            return;

        var oxygen = tile.Air.GetMoles(Gas.Oxygen);

        if (oxygen < 0.5f)
            return;

        var plasma = tile.Air.GetMoles(Gas.Plasma);
        var tritium = tile.Air.GetMoles(Gas.Tritium);

        if (tile.Hotspot.Valid)
        {
            if (soh)
            {
                if (plasma > 0.5f || tritium > 0.5f)
                {
                    tile.Hotspot.Temperature = MathF.Max(tile.Hotspot.Temperature, exposedTemperature);
                    tile.Hotspot.Volume = MathF.Max(tile.Hotspot.Volume, exposedVolume);
                }
            }

            return;
        }

        if (exposedTemperature > Atmospherics.PlasmaMinimumBurnTemperature && (plasma > 0.5f || tritium > 0.5f))
        {
            if (sparkSourceUid.HasValue)
            {
                _adminLog.Add(LogType.Flammable,
                    LogImpact.High,
                    $"Heat/spark of {ToPrettyString(sparkSourceUid.Value)} caused atmos ignition of gas: {tile.Air.Temperature.ToString():temperature}K - {oxygen}mol Oxygen, {plasma}mol Plasma, {tritium}mol Tritium");
            }

            tile.Hotspot = new Hotspot
            {
                Volume = exposedVolume * 25f,
                Temperature = exposedTemperature,
                SkippedFirstProcess = tile.CurrentCycle > gridAtmosphere.UpdateCounter,
                Valid = true,
                State = 1
            };

            AddActiveTile(gridAtmosphere, tile);
            gridAtmosphere.HotspotTiles.Add(tile);
        }
    }

    /// <summary>
    /// Performs hotspot exposure processing on a <see cref="TileAtmosphere"/>.
    /// </summary>
    /// <param name="tile">The <see cref="TileAtmosphere"/> to process.</param>
    private void PerformHotspotExposure(TileAtmosphere tile)
    {
        if (tile.Air == null || !tile.Hotspot.Valid)
            return;

        // Determine if the tile has become a full-blown fire if the volume of the fire has effectively reached
        // the volume of the tile's air.
        tile.Hotspot.Bypassing = tile.Hotspot.SkippedFirstProcess && tile.Hotspot.Volume > tile.Air.Volume * 0.95f;

        // If the tile is effectively a full fire, use the tile's air for reactions, don't bother partitioning.
        if (tile.Hotspot.Bypassing)
        {
            tile.Hotspot.Volume = tile.Air.ReactionResults[(byte)GasReaction.Fire] * Atmospherics.FireGrowthRate;
            tile.Hotspot.Temperature = tile.Air.Temperature;
        }
        // Otherwise, pull out a fraction of the tile's air (the current hotspot volume) to perform reactions on.
        else
        {
            var affected = tile.Air.RemoveVolume(tile.Hotspot.Volume);
            affected.Temperature = tile.Hotspot.Temperature;
            React(affected, tile);
            tile.Hotspot.Temperature = affected.Temperature;
            // Scale the fire based on the type of reaction that occured.
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
}
