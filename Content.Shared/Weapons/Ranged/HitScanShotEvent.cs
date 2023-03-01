namespace Content.Shared.Weapons.Ranged;

/// <summary>
/// Shot may be redirected by changing <see cref="Target"/> variable
/// </summary>
[ByRefEvent]
public record struct HitScanShotEvent(EntityUid? User, EntityUid Target);
