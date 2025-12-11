using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;

[Serializable, NetSerializable]
public sealed class RandomGateBoundUserInterfaceState(float successProbability) : BoundUserInterfaceState
{
    public float SuccessProbability { get; } = successProbability;
}

[Serializable, NetSerializable]
public sealed class RandomGateProbabilityChangedMessage(float probability) : BoundUserInterfaceMessage
{
    public float Probability { get; } = probability;
}

[Serializable, NetSerializable]
public enum RandomGateUiKey : byte
{
    Key
}
