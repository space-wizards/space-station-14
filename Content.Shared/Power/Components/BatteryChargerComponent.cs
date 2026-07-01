using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

/// <summary>
///     Connects the loading side of a <see cref="BatteryComponent"/> to a non-APC power network.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BatteryChargerComponent : Component;
