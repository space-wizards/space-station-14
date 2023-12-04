using Content.Server.Power.EntitySystems;

namespace Content.Server.Power.Components;

/// <summary>
///     Used to flag a player entity that has one or more power monitoring console UIs open at present
/// </summary>
[RegisterComponent, Access(typeof(PowerMonitoringConsoleSystem))]
public sealed partial class PowerMonitoringConsoleUserComponent : Component
{

}
