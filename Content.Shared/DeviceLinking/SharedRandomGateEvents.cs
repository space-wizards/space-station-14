using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;

/// <summary>
/// Message requesting a change to a random gate's output probability.
/// </summary>
[Serializable, NetSerializable]
public sealed class RandomGateProbabilityChangedMessage(float probability) : BoundUserInterfaceMessage
{
    public float Probability = probability;
}

/// <summary>
/// Bound user interface key for a random gate.
/// </summary>
[Serializable, NetSerializable]
public enum RandomGateUiKey : byte
{
    Key
}
