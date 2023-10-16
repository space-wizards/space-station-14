using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Gateway;

[Serializable, NetSerializable]
public enum GatewayVisuals : byte
{
    Active
}

[Serializable, NetSerializable]
public enum GatewayVisualLayers : byte
{
    Portal
}

[Serializable, NetSerializable]
public enum GatewayUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class GatewayBoundUserInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// List of enabled destinations and information about them.
    /// </summary>
    public readonly List<GatewayDestinationData> Destinations;

    /// <summary>
    /// Which destination it is currently linked to, if any.
    /// </summary>
    public readonly NetEntity? Current;

    /// <summary>
    /// Time the portal last opened at.
    /// </summary>
    public readonly TimeSpan LastOpen;

    public GatewayBoundUserInterfaceState(List<GatewayDestinationData> destinations,
        NetEntity? current, TimeSpan nextClose, TimeSpan lastOpen)
    {
        Destinations = destinations;
        Current = current;
        LastOpen = lastOpen;
    }
}

[Serializable, NetSerializable]
public record struct GatewayDestinationData
{
    public NetEntity Entity;

    public FormattedMessage Name;

    public bool Portal;
}

[Serializable, NetSerializable]
public sealed class GatewayOpenPortalMessage : BoundUserInterfaceMessage
{
    public NetEntity Destination;

    public GatewayOpenPortalMessage(NetEntity destination)
    {
        Destination = destination;
    }
}
