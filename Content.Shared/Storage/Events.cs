using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage;

[Serializable, NetSerializable]
public sealed partial class AreaPickupDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public IReadOnlyList<NetEntity> Entities = default!;

    public AreaPickupDoAfterEvent(List<NetEntity> entities)
    {
        Entities = entities;
    }

    public override DoAfterEvent Clone() => this;
}
