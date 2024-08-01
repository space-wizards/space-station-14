using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Radio;

[Serializable, NetSerializable]
public enum IntercomUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ToggleIntercomMicMessage : BoundUserInterfaceMessage
{
    public bool Enabled;

    public ToggleIntercomMicMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class ToggleIntercomSpeakerMessage : BoundUserInterfaceMessage
{
    public bool Enabled;

    public ToggleIntercomSpeakerMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class SelectIntercomChannelMessage : BoundUserInterfaceMessage
{
    public string Channel;

    public SelectIntercomChannelMessage(string channel)
    {
        Channel = channel;
    }
}
