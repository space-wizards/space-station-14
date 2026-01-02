using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;

[Serializable, NetSerializable]
public sealed class RandomGateBoundUserInterfaceState(float successProbability) : BoundUserInterfaceState
{
    public float SuccessProbability = successProbability;
}

[Serializable, NetSerializable]
public sealed class RandomGateProbabilityChangedMessage(float probability) : BoundUserInterfaceMessage
{
    public float Probability = probability;
}

[Serializable, NetSerializable]
public enum RandomGateUiKey : byte
{
    Key
}
