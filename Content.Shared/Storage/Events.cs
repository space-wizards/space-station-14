using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage;

[Serializable, NetSerializable]
public sealed class AreaPickupDoAfterEvent : DoAfterEvent
{
    [DataField("entities", required: true)]
    public readonly IReadOnlyList<EntityUid> Entities = default!;

    private AreaPickupDoAfterEvent()
    {
    }

    public AreaPickupDoAfterEvent(List<EntityUid> entities)
    {
        Entities = entities;
    }

    public override DoAfterEvent Clone() => this;
}
