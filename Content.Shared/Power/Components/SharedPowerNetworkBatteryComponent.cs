namespace Content.Shared.Power.Components;

public abstract partial class SharedPowerNetworkBatteryComponent : Component
{
    [ViewVariables]
    public virtual bool PowerDisabled { get; set; }
}
