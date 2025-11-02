namespace Content.Server.Atmos;

/// <summary>
/// Stores charged electrovae effect state for a tile.
/// Similar to Hotspot, this is a struct stored directly on TileAtmosphere.
/// </summary>
public struct ChargedElectrovaeEffect
{
    /// <summary>
    /// Whether this tile has an active charged electrovae effect.
    /// </summary>
    [ViewVariables]
    public bool Active;

    /// <summary>
    /// Intensity of the effect (0.0 to 1.0), based on charged electrovae concentration.
    /// </summary>
    [ViewVariables]
    public float Intensity;

    /// <summary>
    /// Visual state for overlay effects (0-3).
    /// </summary>
    [ViewVariables]
    public byte State;
}
