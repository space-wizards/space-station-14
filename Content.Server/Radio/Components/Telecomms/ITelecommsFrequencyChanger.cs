namespace Content.Server.Radio.Components.Telecomms;

/// <summary>
/// A telecomms machine that can change frequencies based on if FrequencyToChange is set
/// </summary>
public interface ITelecommsFrequencyChanger : IComponent
{
    /// <summary>
    /// If set, will change the incoming message packet to this set frequency.
    /// </summary>
    int? FrequencyToChange { get; set; }
}
