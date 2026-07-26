namespace Content.Shared.Administration.Logs.Payloads;

/// <summary>
/// Payload for melee, projectile, hitscan, and thrown-item hit events.
/// Covers <c>LogType.MeleeHit</c>, <c>LogType.BulletHit</c>, <c>LogType.HitScanHit</c>,
/// <c>LogType.ThrowHit</c>, and <c>LogType.Electrocution</c>.
/// </summary>
/// <remarks>
/// Actor and victim are in participant rows; do not duplicate their UIDs here.
/// </remarks>
/// <param name="WeaponPrototype"> Weapon prototype ID, e.g. <c>"WeaponCrowbar"</c>. Null for unarmed.</param>
/// <param name="WeaponDisplayName">Snapshot display name for historical context. Null for unarmed.</param>
/// <param name="DamageByType">Per-damage-type breakdown. May be empty for hits that deal no damage.</param>
/// <param name="TotalDamage">Total damage dealt as <c>FixedPoint2.Int()</c>.</param>
/// <param name="HitResult">Outcome: <c>"Hit"</c>, <c>"Graze"</c>, or <c>"Miss"</c>.</param>
/// <param name="ProjectilePrototype"> Projectile prototype ID when applicable. Null for melee/unarmed.</param>
public sealed record CombatHitLogPayload(
    string? WeaponPrototype,
    string? WeaponDisplayName,
    IReadOnlyList<DamageEntrySnapshot> DamageByType,
    int TotalDamage,
    string HitResult,
    string? ProjectilePrototype = null) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}
