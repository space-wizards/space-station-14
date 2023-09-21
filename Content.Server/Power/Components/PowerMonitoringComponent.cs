using Content.Shared.Power;

namespace Content.Server.Power.Components;

[RegisterComponent]
public abstract partial class PowerMonitoringComponent : Component
{
    [DataField("sourceNode")]
    public string SourceNode = "hv";

    [DataField("loadNode")]
    public string LoadNode = "hv";
}

[RegisterComponent]
public sealed partial class PowerMonitoringConsoleComponent : PowerMonitoringComponent
{

}

[RegisterComponent]
public sealed partial class PowerMonitoringDistributorComponent : PowerMonitoringComponent
{

}

public sealed class PowerMonitoringSetUIStateEvent : EntityEventArgs
{
    public readonly EntityUid Entity;
    public readonly PowerMonitoringBoundInterfaceState State;

    public PowerMonitoringSetUIStateEvent(EntityUid entity, PowerMonitoringBoundInterfaceState state)
    {
        Entity = entity;
        State = state;
    }
}

