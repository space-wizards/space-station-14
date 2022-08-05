using Robust.Shared.Serialization;

namespace Content.Shared.Radio;

[Serializable, NetSerializable]
public enum RadioUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class RadioBoundInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// Broadcasting
    /// </summary>
    public readonly bool TX;
    /// <summary>
    /// Listening
    /// </summary>
    public readonly bool RX;
    /// <summary>
    /// Set Frequency
    /// </summary>
    public readonly int Frequency;
    /// <summary>
    /// Can the user edit frequency?
    /// </summary>
    public readonly bool FrequencyLock;
    /// <summary>
    /// Do we have the command loudspeaker?
    /// </summary>
    public readonly bool Command;
    /// <summary>
    /// Use command loudspeaker
    /// </summary>
    public readonly bool UseCommand;
    /// <summary>
    /// All available radiokey channels. Key = channel int., value = channel name (optional)
    /// the int will be sent back for on/off
    /// </summary>
    public readonly Dictionary<int, string> Channels;
    /// <summary>
    /// Blocked freq list.
    /// </summary>
    public readonly HashSet<int> BlockedFrequency;

    /*
  const {
    freqlock,
    frequency,
    minFrequency,
    maxFrequency,
    listening,
    broadcasting,
    command,
    useCommand,
    subspace,
    subspaceSwitchable,
  } = data;
     */

    public RadioBoundInterfaceState(bool tx, bool rx, int frequency, Dictionary<int, string> channels, HashSet<int> blockedFrequency, bool freqLock = false, bool command = false, bool useCommand = false)
    {
        // pretty much the exact same data tgui sends to the ui
        TX = tx;
        RX = rx;
        Frequency = frequency;
        FrequencyLock = freqLock;
        BlockedFrequency = blockedFrequency;
        Channels = channels;
        Command = command;
        UseCommand = useCommand;
    }
}
