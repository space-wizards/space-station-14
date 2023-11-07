using Robust.Shared.Serialization;

namespace Content.Shared.Radio;

[Serializable, NetSerializable]
public enum HandheldRadioUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class HandheldRadioBoundUIState : BoundUserInterfaceState
{
    public bool MicEnabled;
    public bool SpeakerEnabled;
    public List<string> AvailableChannels;
    public string SelectedChannel;

    public HandheldRadioBoundUIState(bool micEnabled, bool speakerEnabled, List<string> availableChannels, string selectedChannel)
    {
        MicEnabled = micEnabled;
        SpeakerEnabled = speakerEnabled;
        AvailableChannels = availableChannels;
        SelectedChannel = selectedChannel;
    }
}

[Serializable, NetSerializable]
public sealed class ToggleHandheldRadioMicMessage : BoundUserInterfaceMessage
{
    public bool Enabled;

    public ToggleHandheldRadioMicMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class ToggleHandheldRadioSpeakerMessage : BoundUserInterfaceMessage
{
    public bool Enabled;

    public ToggleHandheldRadioSpeakerMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class SelectHandheldRadioChannelMessage : BoundUserInterfaceMessage
{
    public string Channel;

    public SelectHandheldRadioChannelMessage(string channel)
    {
        Channel = channel;
    }
}
