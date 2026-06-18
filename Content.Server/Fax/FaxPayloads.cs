using Content.Shared.DeviceNetwork;
using Content.Shared.Fax.Components;

namespace Content.Server.Fax;

/// <summary>
/// Broadcasted from one fax to all other available faxes.
/// </summary>
public sealed partial class FaxPingPayload : NetworkPayload
{
    /// <summary>
    /// I!!!!! AM!!!!! SYNDICATE!!!!!!!!
    /// </summary>
    /// <remarks>
    /// TODO this should probably be made a more general system in the future
    /// </remarks>
    [DataField]
    public bool IsSyndicate;
}

/// <summary>
/// Sent as a response to <see cref="FaxPingPayload"/>.
/// </summary>
public sealed partial class FaxPongPayload : NetworkPayload
{
    [DataField]
    public string FaxName;
}

/// <summary>
/// Payload to print a paper on the receiver fax.
/// </summary>
public sealed partial class FaxPrintPayload : NetworkPayload
{
    [DataField]
    public FaxPrintout Data;
}

// Goobstation
public sealed partial class FaxSendEntityPayload : NetworkPayload
{
    [DataField]
    public EntityUid? Entity;

    [DataField]
    public bool CrossGrids;
}
