namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed partial class PowerMonitoringConsoleComponent : Component
{
    [DataField("sourceNode")]
    public string SourceNode = "hv";

    [DataField("loadNode")]
    public string LoadNode = "hv";
}

