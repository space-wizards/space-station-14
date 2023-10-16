using Robust.Shared.Serialization;

namespace Content.Shared.Gateway;

/// <summary>
/// Sent from client to server upon taking a gateway destination.
/// </summary>
[Serializable, NetSerializable]
public sealed class GatewayDestinationMessage : EntityEventArgs
{
    public int Index;
}
