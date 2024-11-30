using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.CrewMedal;

[Serializable, NetSerializable]
public enum CrewMedalUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CrewMedalReasonChangedMessage(string reason) : BoundUserInterfaceMessage
{
    public string Reason { get; } = reason;
}
