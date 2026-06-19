using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal.Mailing;

[Serializable, NetSerializable]
public sealed partial class MailRequestTagPayload : NetworkPayload;

[Serializable, NetSerializable]
public sealed partial class MailTagPayload : NetworkPayload
{
    [DataField]
    public string Tag;
}

[Serializable, NetSerializable]
public sealed partial class MailSendPayload : NetworkPayload
{
    [DataField]
    public string Tag;

    [DataField]
    public string Target;
}
