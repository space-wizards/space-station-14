namespace Content.Server.Power.Components;

/// <summary>
/// Fetches entity's <see cref="BatteryComponent"/> and unpowers it.
/// Runs at MapInit and removes itself afterwards.
/// </summary>
[RegisterComponent]
public sealed partial class UnpowerOnMapInitComponent : Component
{
}
