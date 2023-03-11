namespace Content.Shared.Weapons.Ranged;

/// <summary>
/// Shot may be reflected by setting <see cref="Reflected"/> to true
/// and changing angle value
/// </summary>
[ByRefEvent]
public record struct HitScanReflectAttempt(Vector2 Direction, bool Reflected);
