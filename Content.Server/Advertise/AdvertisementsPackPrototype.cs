using Robust.Shared.Prototypes;

namespace Content.Server.Advertise;

[Serializable, Prototype("advertisementsPack")]
public sealed partial class AdvertisementsPackPrototype : MessagePackPrototype
{
    [Obsolete("Convert to MessagePack")]
    public List<string> Advertisements => Messages;
}
