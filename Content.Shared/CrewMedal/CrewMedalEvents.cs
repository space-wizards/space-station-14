using Robust.Shared.Serialization;

namespace Content.Shared.CrewMedal;

[Serializable, NetSerializable]
public enum CrewMedalUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CrewMedalReasonChangedMessage(string reason) : BoundUserInterfaceMessage
{
    public string Reason { get; } = reason;
}
