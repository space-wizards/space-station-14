using Robust.Shared.Serialization;

namespace Content.Shared.Radio;

[Serializable, NetSerializable]
public sealed class RadioToggleFrequencyFilter : BoundUserInterfaceMessage
{
    public int Frequency { get; set; }
}

[Serializable, NetSerializable]
public sealed class RadioChangeFrequency : BoundUserInterfaceMessage
{
    public int Frequency { get; set; }
}

[Serializable, NetSerializable]
public sealed class RadioToggleTX : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class RadioToggleRX : BoundUserInterfaceMessage
{
}
