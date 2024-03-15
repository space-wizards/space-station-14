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
    public int Frequency;

    public HandheldRadioBoundUIState(bool micEnabled, bool speakerEnabled, int frequency)
    {
        MicEnabled = micEnabled;
        SpeakerEnabled = speakerEnabled;
        Frequency = frequency;
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
public sealed class SelectHandheldRadioFrequencyMessage : BoundUserInterfaceMessage
{
    public int Frequency;

    public SelectHandheldRadioFrequencyMessage(string frequency)
    {
        int.TryParse(frequency.Trim(), out Frequency);
    }
}
