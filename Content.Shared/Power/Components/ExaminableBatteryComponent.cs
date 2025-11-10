using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

/// <summary>
/// Allows the charge of a battery to be seen by examination.
/// Works with either  <see cref="BatteryComponent"/> or <see cref="PredictedBatteryComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ExaminableBatteryComponent : Component;
