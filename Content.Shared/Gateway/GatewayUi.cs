using Robust.Shared.Serialization;

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
    public readonly List<(EntityUid, string, TimeSpan, bool)> Destinations;

    /// <summary>
    /// Which destination it is currently linked to, if any.
    /// </summary>
    public readonly EntityUid? Current;

    /// <summary>
    /// Time the portal will close at.
    /// </summary>
    public readonly TimeSpan NextClose;

    /// <summary>
    /// Time the portal last opened at.
    /// </summary>
    public readonly TimeSpan LastOpen;

    public GatewayBoundUserInterfaceState(List<(EntityUid, string, TimeSpan, bool)> destinations,
        EntityUid? current, TimeSpan nextClose, TimeSpan lastOpen)
    {
        Destinations = destinations;
        Current = current;
        NextClose = nextClose;
        LastOpen = lastOpen;
    }
}

[Serializable, NetSerializable]
public sealed class GatewayOpenPortalMessage : BoundUserInterfaceMessage
{
    public EntityUid Destination;

    public GatewayOpenPortalMessage(EntityUid destination)
    {
        Destination = destination;
    }
}
