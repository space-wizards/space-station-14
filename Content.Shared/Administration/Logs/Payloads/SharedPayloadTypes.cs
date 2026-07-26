namespace Content.Shared.Administration.Logs.Payloads;

/// <summary>
/// A single damage-type/amount pair, used in <see cref="CombatHitLogPayload"/> and
/// <see cref="CombatDamageLogPayload"/>.
/// </summary>
/// <param name="DamageType">
/// Prototype ID of the damage type, e.g. <c>"Blunt"</c>, <c>"Slash"</c>,
/// <c>"Heat"</c>.  Never a display string.
/// </param>
/// <param name="Amount">
/// Damage amount as <c>FixedPoint2.Int()</c>.
/// </param>
public sealed record DamageEntrySnapshot(
    string DamageType,
    int Amount);

/// <summary>
/// A single reagent/volume pair used in <see cref="ChemistryInjectionLogPayload"/>.
/// </summary>
/// <param name="ReagentPrototype">Reagent prototype ID.</param>
/// <param name="Quantity">
/// Volume as <c>FixedPoint2.Int()</c>.
/// </param>
public sealed record ReagentSnapshot(
    string ReagentPrototype,
    int Quantity);
