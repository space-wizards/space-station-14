using Robust.Shared.Serialization;

namespace Content.Shared.Body.Organ;

[Serializable, NetSerializable]
public sealed record OrganSlot(string Id, EntityUid Parent)
{
    public EntityUid? Child { get; set; }
}
