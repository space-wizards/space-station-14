using Content.Shared.Power.Components;

namespace Content.Server.Power.Components;

/// <summary>
///     Connects the loading side of a <see cref="BatteryComponent"/> to a non-APC power network.
/// </summary>
[RegisterComponent]
public sealed partial class BatteryChargerComponent : Component;
