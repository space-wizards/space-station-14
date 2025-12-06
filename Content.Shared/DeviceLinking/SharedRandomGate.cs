using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;

[Serializable, NetSerializable]
public sealed class RandomGateBoundUserInterfaceState : BoundUserInterfaceState
{
    public float SuccessProbability { get; }

    public RandomGateBoundUserInterfaceState(float successProbability)
    {
        SuccessProbability = successProbability;
    }
}

[Serializable, NetSerializable]
public sealed class RandomGateProbabilityChangedMessage : BoundUserInterfaceMessage
{
    public float Probability { get; }

    public RandomGateProbabilityChangedMessage(float probability)
    {
        Probability = probability;
    }
}

[Serializable, NetSerializable]
public enum RandomGateUiKey : byte
{
    Key
}
