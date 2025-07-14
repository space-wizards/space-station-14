using Content.Server.Atmos.EntitySystems;
using Content.Server.Emp;
using Content.Shared.Atmos;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server.Atmos.Reactions;

/// <summary>
/// Manages charged electrovae EMP effects in a batch.
/// </summary>
/// <remarks>
/// This system batches processing of charged electrovae reactions to avoid
/// expensive entity lookups during gas reaction ticks. It processes
/// accumulated reactions at controlled intervals for better performance.
/// </remarks>
public sealed class ChargedElectrovaeManager : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    
    // Cache of active tiles with charged electrovae reactions
    private readonly Dictionary<(EntityUid Grid, Vector2i Position), (TimeSpan LastPulse, float Intensity)> _activeTiles = [];

    // Process less frequently than gas reactions (every 0.5 seconds)
    private const float ProcessInterval = 0.5f;
    private float _accumulator = 0f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(MapInitEvent ev)
    {
        _activeTiles.Clear();
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator < ProcessInterval)
            return;

        _accumulator -= ProcessInterval;
        ProcessEmpEffects();
    }

    /// <summary>
    /// Registers a tile with charged electrovae for batch EMP processing.
    /// </summary>
    /// <param name="gridId">Grid entity UID</param>
    /// <param name="position">Position on the grid</param>
    /// <param name="intensity">Reaction intensity (0.2-1.0)</param>
    public void RegisterTile(EntityUid gridId, Vector2i position, float intensity)
    {
        // If the entry doesn't exist, create a new entry
        if (!_activeTiles.TryGetValue((gridId, position), out _))
        {
            _activeTiles[(gridId, position)] = (_timing.CurTime, intensity);
        }
    }

    /// <summary>
    /// Checks if a tile still has enough charged electrovae to trigger an EMP
    /// </summary>
    /// <param name="gridId">The grid entity UID</param>
    /// <param name="tilePos">The tile position</param>
    /// <returns>True if the tile has enough charged electrovae, false otherwise</returns>
    private bool TileShouldTriggerEmp(EntityUid gridId, Vector2i tilePos)
    {
        if (!_atmosphereSystem.HasAtmosphere(gridId))
            return false;

        var mixture = _atmosphereSystem.GetTileMixture(gridId, null, tilePos, true);
        if (mixture == null)
            return false;

        return mixture.GetMoles(Gas.ChargedElectrovae) >= Atmospherics.ChargedElectrovaeMinimumAmount;
    }
    
    /// <summary>
    /// Consume gases required for the EMP effect
    /// </summary>
    /// <param name="gridId">The grid entity UID</param>
    /// <param name="tilePos">The tile position</param>
    /// <returns>True if gases were consumed, false if insufficient gases</returns>
    private bool ConsumeGasesForEmp(EntityUid gridId, Vector2i tilePos)
    {
        var mixture = _atmosphereSystem.GetTileMixture(gridId, null, tilePos, true);
        if (mixture == null)
            return false;
            
        var ceToConsume = Atmospherics.ChargedElectrovaeMinimumAmount;
        var oxygenToConsume = ceToConsume * Atmospherics.ChargedElectrovaeOxygenEmpRatio;
        
        if (mixture.GetMoles(Gas.ChargedElectrovae) < ceToConsume)
            return false;
            
        oxygenToConsume = Math.Min(oxygenToConsume, mixture.GetMoles(Gas.Oxygen));
        
        mixture.AdjustMoles(Gas.ChargedElectrovae, -ceToConsume);
        mixture.AdjustMoles(Gas.Oxygen, -oxygenToConsume);
        
        return true;
    }

    /// <summary>
    /// Process all accumulated charged electrovae EMP effects.
    /// </summary>
    private void ProcessEmpEffects()
    {
        if (_activeTiles.Count == 0)
            return;

        // Process all active tiles in one batch
        // Group by grid to minimize grid lookups
        var byGrid = _activeTiles.GroupBy(kv => kv.Key.Grid);
        var currentTime = _timing.CurTime;
        
        // Create a list to track tiles to remove from the cache (tiles without sufficient charged electrovae or timed out)
        var tilesToRemove = new List<(EntityUid, Vector2i)>();

        foreach (var gridGroup in byGrid)
        {
            var gridId = gridGroup.Key;

            if (Deleted(gridId) || !HasComp<MapGridComponent>(gridId))
                continue;

            var grid = Comp<MapGridComponent>(gridId);

            // Process all tiles for this grid at once
            foreach (var ((currentGrid, tilePos), (lastPulse, intensity)) in gridGroup)
            {
                // Skip if not enough time has passed since last pulse
                var timeSinceLastPulse = currentTime - lastPulse;
                var cooldownTime = TimeSpan.FromSeconds(Atmospherics.ChargedElectrovaeCooldown + intensity);
                if (timeSinceLastPulse < cooldownTime)
                    continue;

                // Skip if the grid or map doesn't exist anymore
                if (Deleted(currentGrid) || !TryComp(currentGrid, out MapGridComponent? currentGridComp))
                    continue;
                    
                // Check if the tile still has enough charged electrovae to trigger an EMP
                if (!TileShouldTriggerEmp(currentGrid, tilePos))
                {
                    // No need to keep this tile in the cache anymore - it will be re-added if more charged electrovae appears
                    tilesToRemove.Add((currentGrid, tilePos));
                    continue;
                }

                // Get entity coordinates at the center of the tile
                var entityCoords = _mapSystem.GridTileToLocal(currentGrid, currentGridComp, tilePos);
                
                // Convert entity coordinates to map coordinates using the recommended method
                var mapCoords = _transform.ToMapCoordinates(entityCoords);
                
                // Consume gases before triggering EMP
                if (!ConsumeGasesForEmp(currentGrid, tilePos))
                {
                    // Not enough gas to consume, remove from active tiles
                    tilesToRemove.Add((currentGrid, tilePos));
                    continue;
                }
                
                // intensity is between 0.04 and 1.0
                var powerScale = Math.Min(intensity * 2, 1.0f);
                var empRadius = Atmospherics.ChargedElectrovaeEmpRadius;
                var empEnergy = Atmospherics.ChargedElectrovaeEmpEnergy * powerScale;
                var empDuration = Atmospherics.ChargedElectrovaeEmpDuration * powerScale;
                
                // Trigger EMP pulse at the tile position with enhanced parameters
                _emp.EmpPulse(mapCoords, empRadius, empEnergy, empDuration);

                tilesToRemove.Add((currentGrid, tilePos));
            }
        }

        // Add old entries (more than 9 seconds old) to removal list
        foreach (var kv in _activeTiles)
        {
            if ((currentTime - kv.Value.LastPulse) > TimeSpan.FromSeconds(9))
            {
                tilesToRemove.Add(kv.Key);
            }
        }
        
        // Remove all tiles that no longer have charged electrovae or are too old
        foreach (var key in tilesToRemove)
        {
            _activeTiles.Remove(key);
        }
    }
}
