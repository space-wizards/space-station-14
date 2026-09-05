namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on the target when successfully struck by a hitscan attack.
/// </summary>
[ByRefEvent]
public record struct HitByHitScanEvent;
