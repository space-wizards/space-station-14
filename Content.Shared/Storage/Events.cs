using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage;

[Serializable, NetSerializable]
public sealed partial class AreaPickupDoAfterEvent : DoAfterEvent
{
    [DataField("entities", required: true)]
    public IReadOnlyList<NetEntity> Entities = default!;

    private AreaPickupDoAfterEvent()
    {
    }

    public AreaPickupDoAfterEvent(List<NetEntity> entities)
    {
        Entities = entities;
    }

    public override DoAfterEvent Clone() => this;
}

/// <summary>
/// This is raised on the storage entity on an attempt to open it.
/// </summary>
public sealed class StorageOpenUIAttemptEvent : CancellableEntityEventArgs
{
    public EntityUid User { get; }
    public StorageOpenUIAttemptEvent(EntityUid who)
    {
        User = who;
    }
}
