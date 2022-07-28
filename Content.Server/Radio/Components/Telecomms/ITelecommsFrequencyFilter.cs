namespace Content.Server.Radio.Components.Telecomms;

/// <summary>
/// A thing which can filter to listen only in set frequencies.
/// </summary>
public interface ITelecommsFrequencyFilter : IComponent
{
    /// <summary>
    /// List of frequencies we are listening to. If nonexistant then everything is VALID
    /// </summary>
    List<int> ListeningFrequency { get; set; }
    /// <summary>
    /// Can we send a packet to this thing?
    /// </summary>
    /// <param name="freq"></param>
    /// <returns></returns>
    bool IsFrequencyListening(int freq);
}
