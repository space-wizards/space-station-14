using Content.Shared.DeviceNetwork;

namespace Content.Shared.Disposal.Mailing;

public sealed partial class MailRequestTagPayload : NetworkPayload;

public sealed partial class MailTagPayload : NetworkPayload
{
    [DataField]
    public string Tag;
}

public sealed partial class MailSendPayload : NetworkPayload
{
    [DataField]
    public string Tag;

    [DataField]
    public string Target;
}
