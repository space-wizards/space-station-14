namespace Content.Shared.Weapons.Ranged;

/// <summary>
/// Shot may be reflected by setting <see cref="Reflected"/> to true
/// </summary>
[ByRefEvent]
public record struct HitScanReflectAttempt(bool Reflected);
