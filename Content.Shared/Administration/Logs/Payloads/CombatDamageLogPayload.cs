namespace Content.Shared.Administration.Logs.Payloads;

/// <summary>
/// Payload for generic damage and healing events.
/// Covers <c>LogType.Damaged</c>, <c>LogType.Healed</c>, <c>LogType.Stamina</c>,
/// <c>LogType.Gib</c>, and <c>LogType.ExplosionHit</c>.
/// </summary>
/// <param name="SourcePrototype">Prototype of the damage source if known. Null if unknown.</param>
/// <param name="DamageGroup"> Damage type, e.g. <c>"Brute"</c>, <c>"Burn"</c>, <c>"Toxin"</c>. Null if mixed.</param>
/// <param name="DamageByType">Per-damage-type breakdown.</param>
/// <param name="TotalDamage">Total damage dealt/healed.</param>
/// <param name="NewMobState">The mob state reached as a result of this damage event, or null if no state transition occurred.</param>
public sealed record CombatDamageLogPayload(
    string? SourcePrototype,
    string? DamageGroup,
    IReadOnlyList<DamageEntrySnapshot> DamageByType,
    int TotalDamage,
    string? NewMobState = null) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}
