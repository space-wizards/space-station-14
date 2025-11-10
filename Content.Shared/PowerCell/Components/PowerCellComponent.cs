using Content.Shared.Power.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.PowerCell.Components;

/// <summary>
/// This component enables power-cell related interactions (e.g. EntityWhitelists, cell sizes, examine, rigging).
/// The actual power functionality is provided by the <see cref="PredictedBatteryComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PowerCellComponent : Component;
