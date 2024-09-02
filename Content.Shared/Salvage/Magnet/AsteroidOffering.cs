using Content.Shared.Procedural;

namespace Content.Shared.Salvage.Magnet;

/// <summary>
/// Asteroid offered for the magnet.
/// </summary>
public record struct AsteroidOffering : ISalvageMagnetOffering
{
    public string Id;

    public DungeonConfig DungeonConfig;

    /// <summary>
    /// Calculated marker layers for the asteroid.
    /// </summary>
    public Dictionary<string, int> MarkerLayers;
}
