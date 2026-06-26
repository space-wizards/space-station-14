using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal.Mailing;

[Serializable, NetSerializable]
public sealed partial class MailRequestTagPayload : HandledNetworkPayload;

[Serializable, NetSerializable]
public sealed partial class MailTagPayload : HandledNetworkPayload
{
    [DataField]
    public string Tag;
}

[Serializable, NetSerializable]
public sealed partial class MailSendPayload : HandledNetworkPayload
{
    [DataField]
    public string Tag;

    [DataField]
    public string Target;
}
