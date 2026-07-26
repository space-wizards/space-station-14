using Content.Shared.DeviceNetwork;
using Content.Shared.Fax.Components;

namespace Content.Shared.Fax;

/// <summary>
/// Broadcasted from one fax to all other available faxes.
/// </summary>
public sealed partial class FaxPingPayload : NetworkPayloadBase<FaxPingPayload>
{
    // TODO this should probably be made a more general system in the future
    [DataField]
    public bool IsSyndicate;
}

/// <summary>
/// Sent as a response to <see cref="FaxPingPayload"/>.
/// </summary>
public sealed partial class FaxPongPayload : NetworkPayloadBase<FaxPongPayload>
{
    [DataField]
    public string FaxName;
}

/// <summary>
/// Payload to print a paper on the receiver fax.
/// </summary>
public sealed partial class FaxPrintPayload : NetworkPayloadBase<FaxPrintPayload>
{
    [DataField]
    public FaxPrintout Data;
}
