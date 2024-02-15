using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

/// <summary>
/// Raised when the client changes the shuttle console's thrust limit.
/// </summary>
[Serializable, NetSerializable]
public sealed class ThrustLimitedMessage : BoundUserInterfaceMessage
{
    public float ThrustLimit;
}
