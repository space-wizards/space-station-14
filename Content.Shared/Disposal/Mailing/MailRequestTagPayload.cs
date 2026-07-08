using Content.Shared.DeviceNetwork;

namespace Content.Shared.Disposal.Mailing;

public sealed partial class MailRequestTagPayload : NetworkPayloadBase<MailRequestTagPayload>;

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
