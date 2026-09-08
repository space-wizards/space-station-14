using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

/// <summary>
/// This entity is currently inside the charging slot of an entity with <see cref="ChargerComponent"/>.
/// Added regardless whether or not the charger is powered.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InsideChargerComponent : Component;
