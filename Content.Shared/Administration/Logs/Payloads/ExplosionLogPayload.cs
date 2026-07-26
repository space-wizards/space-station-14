namespace Content.Shared.Administration.Logs.Payloads;

/// <summary>
/// Payload for explosion events (<c>LogType.Explosion</c>, <c>LogType.ExplosionHit</c>,
/// <c>LogType.Trigger</c>).
/// </summary>
/// <remarks>
/// The triggering entity is captured as a participant.
/// </remarks>
/// <param name="ExplosionType">Explosion prototype ID, e.g. <c>"Default"</c>, <c>"Nuclear"</c>.</param>
/// <param name="TotalIntensity">Total explosion intensity.</param>
/// <param name="Slope">Intensity falloff slope.</param>
/// <param name="Radius">Effective blast radius.</param>
/// <param name="CoordX">World X coordinate of the epicentre.</param>
/// <param name="CoordY">World Y coordinate of the epicentre.</param>
/// <param name="MapId">Map identifier, or null when the map cannot be determined.</param>
public sealed record ExplosionLogPayload(
    string ExplosionType,
    double TotalIntensity,
    double Slope,
    double Radius,
    float CoordX,
    float CoordY,
    int? MapId) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}
