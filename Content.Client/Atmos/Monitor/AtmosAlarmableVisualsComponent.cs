using Content.Shared.Atmos.Monitor;

namespace Content.Client.Atmos.Monitor;

[RegisterComponent]
public sealed class AtmosAlarmableVisualsComponent : Component
{
    [DataField("layerMap")]
    public string LayerMap { get; } = string.Empty;

    [DataField("alarmStates")]
    public readonly Dictionary<AtmosAlarmType, string> AlarmStates = new();

    [DataField("hideOnDepowered")]
    public readonly List<string>? HideOnDepowered;

    // eh...
    [DataField("setOnDepowered")]
    public readonly Dictionary<string, string>? SetOnDepowered;
}
