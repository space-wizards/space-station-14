using Robust.Shared.GameStates;

namespace Content.Shared.Administration.Components;

/// <summary>
/// This is used for the admin map-wide/station-wide/grid-wide infinite power trick.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StationInfiniteBatteryTargetComponent : Component;
