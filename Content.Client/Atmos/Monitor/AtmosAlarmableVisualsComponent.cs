using Content.Shared.Atmos.Monitor;

namespace Content.Client.Atmos.Monitor;

[RegisterComponent]
public sealed partial class AtmosAlarmableVisualsComponent : Component
{
    [DataField]
    public string LayerMap { get; private set; } = string.Empty;

    [DataField]
    public Dictionary<AtmosAlarmType, string> AlarmStates = new();

    [DataField]
    public List<string>? HideOnDepowered;

    // eh...
    [DataField]
    public Dictionary<string, string>? SetOnDepowered;
}
