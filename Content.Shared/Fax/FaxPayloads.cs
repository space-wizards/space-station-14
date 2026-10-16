using Content.Shared.DeviceNetwork;

namespace Content.Shared.Fax;

/// <summary>
/// Broadcasted from one fax to all other available faxes.
/// </summary>
public partial record struct FaxPingPayload(string FaxName, bool IsSyndicate) : INetworkPayload
{
    [DataField]
    public string FaxName = FaxName;

    // TODO: this should probably be made a more general system in the future
    // TODO: Bitmask flags?
    [DataField]
    public bool IsSyndicate = IsSyndicate;
}

/// <summary>
/// Sent as a response to <see cref="FaxPingPayload"/>.
/// </summary>
public partial record struct FaxPongPayload(string FaxName) : INetworkPayload
{
    [DataField]
    public string FaxName = FaxName;
}

/// <summary>
/// Sent when a fax machine shuts down, removes this device from other devices!
/// </summary>
public partial record struct FaxShutdownPayload : INetworkPayload;
