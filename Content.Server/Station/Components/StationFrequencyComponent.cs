namespace Content.Server.Station.Components;

/// <summary>
/// Adjusts the frequency of the crew headsets.
/// </summary>
[RegisterComponent]
public sealed partial class StationFrequencyComponent : Component
{
    [DataField("frequency")]
    public Dictionary<string, int> Frequency = new();
}
