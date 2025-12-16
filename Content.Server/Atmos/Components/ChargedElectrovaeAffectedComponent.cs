namespace Content.Server.Atmos.Components;

/// <summary>
/// Marks an entity as currently affected by charged electrovae gas.
/// Used to track and restore battery capacity and power states when the entity
/// leaves the gas or the gas dissipates.
/// </summary>
[RegisterComponent]
public sealed partial class ChargedElectrovaeAffectedComponent : Component;
