namespace Content.Server.Station.Components;

/// <summary>
/// Allows to set the frequency of all outgoing messages from the grid for specific channels.
/// </summary>
[RegisterComponent]
public sealed partial class StationFrequencyComponent : Component
{
    [DataField]
    public Dictionary<string, int> Frequency = new();
}
