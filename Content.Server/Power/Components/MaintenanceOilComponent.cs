namespace Content.Server.Power.Components;

/// <summary>
/// Add this to a substation so that it requires maintenance (by replenishing
/// with oil). Also needs a SolutionContainerManager, a
/// BatteryDischargerComponent, and a DamagableComponent in order for this to
/// actually do anything.
/// </summary>
[RegisterComponent]
public sealed class MaintenanceOilComponent : Component
{
}
