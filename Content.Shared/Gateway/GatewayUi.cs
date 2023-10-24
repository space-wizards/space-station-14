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
    /// Next time the portal is ready to be used.
    /// </summary>
    public readonly TimeSpan NextReady;

    public readonly TimeSpan Cooldown;

    public GatewayBoundUserInterfaceState(List<GatewayDestinationData> destinations,
        NetEntity? current, TimeSpan nextReady, TimeSpan cooldown)
    {
        Destinations = destinations;
        Current = current;
        NextReady = nextReady;
        Cooldown = cooldown;
    }
}

[Serializable, NetSerializable]
public record struct GatewayDestinationData
{
    public NetEntity Entity;

    public FormattedMessage Name;

    public TimeSpan NextReady;

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
