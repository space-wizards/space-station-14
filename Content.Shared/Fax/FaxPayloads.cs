using Content.Shared.DeviceNetwork;
using Content.Shared.Fax.Components;

namespace Content.Shared.Fax;

/// <summary>
/// Broadcasted from one fax to all other available faxes.
/// </summary>
public partial record struct FaxPingPayload : INetworkPayload
{
    // TODO this should probably be made a more general system in the future
    [DataField]
    public bool IsSyndicate;
}

/// <summary>
/// Sent as a response to <see cref="FaxPingPayload"/>.
/// </summary>
public partial record struct FaxPongPayload : INetworkPayload
{
    [DataField]
    public string FaxName;
}
