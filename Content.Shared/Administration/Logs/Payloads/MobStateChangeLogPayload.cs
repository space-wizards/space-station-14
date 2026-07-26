namespace Content.Shared.Administration.Logs.Payloads;

/// <summary>
/// Payload for mob state transition events.
/// Used with <c>LogType.MobStateChange</c> (to be added).
/// </summary>
/// <param name="PreviousState">Mob state before the transition: <c>"Alive"</c>, <c>"Critical"</c>, or <c>"Dead"</c>.</param>
/// <param name="NewState">Mob state after the transition.</param>
/// <param name="CausePrototype"> Prototype ID of the weapon or hazard that caused the final damage, if known.
/// Null for environmental or unknown causes.
/// </param>
/// <param name="CauseCategory">
/// Broad cause category for filtering: <c>"Melee"</c>, <c>"Projectile"</c>,
/// <c>"Explosion"</c>, <c>"Environmental"</c>, or <c>"Unknown"</c>.
/// </param>
public sealed record MobStateChangeLogPayload(
    string PreviousState,
    string NewState,
    string? CausePrototype = null,
    string? CauseCategory = null) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}
