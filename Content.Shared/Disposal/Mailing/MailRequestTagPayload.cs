using Content.Shared.DeviceNetwork;

namespace Content.Shared.Disposal.Mailing;

/// <summary>
/// Request to get all available target mailing units.
/// </summary>
public sealed partial class MailRequestTagPayload : NetworkPayloadBase<MailRequestTagPayload>;

/// <summary>
/// Sent as response to <see cref="MailRequestTagPayload"/>, contains tag of the sender.
/// </summary>
public sealed partial class MailTagPayload : NetworkPayloadBase<MailTagPayload>
{
    [DataField]
    public string Tag;
}

public sealed partial class MailSendPayload : NetworkPayloadBase<MailSendPayload>
{
    [DataField]
    public string Tag;

    [DataField]
    public string Target;
}
