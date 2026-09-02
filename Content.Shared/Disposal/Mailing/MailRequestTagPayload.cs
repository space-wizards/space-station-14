using Content.Shared.DeviceNetwork;

namespace Content.Shared.Disposal.Mailing;

/// <summary>
/// Request to get all available target mailing units.
/// </summary>
public partial record struct MailRequestTagPayload : INetworkPayload;

/// <summary>
/// Sent as response to <see cref="MailRequestTagPayload"/>, contains tag of the sender.
/// </summary>
public partial record struct MailTagPayload : INetworkPayload
{
    [DataField]
    public string Tag;
}

public partial record struct MailSendPayload : INetworkPayload
{
    [DataField]
    public string Tag;

    [DataField]
    public string Target;
}
