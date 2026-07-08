using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal.Mailing;

[Serializable, NetSerializable]
public sealed partial class MailRequestTagPayload : NetworkPayloadBase<MailRequestTagPayload>;

[Serializable, NetSerializable]
public sealed partial class MailTagPayload : NetworkPayloadBase<MailTagPayload>
{
    [DataField]
    public string Tag;
}

[Serializable, NetSerializable]
public sealed partial class MailSendPayload : NetworkPayloadBase<MailSendPayload>
{
    [DataField]
    public string Tag;

    [DataField]
    public string Target;
}
