using Content.Shared.Physics;
using Content.Shared.Sound;
using Robust.Shared.Containers;

namespace Content.Server.Storage.Components;

[RegisterComponent]
public sealed class EntityStorageComponent : Component
{
    public readonly float MaxSize = 1.0f; // maximum width or height of an entity allowed inside the storage.

    public static readonly TimeSpan InternalOpenAttemptDelay = TimeSpan.FromSeconds(0.5);
    public TimeSpan LastInternalOpenAttempt;

    /// <summary>
    ///     Collision masks that get removed when the storage gets opened.
    /// </summary>
    public readonly int MasksToRemove = (int) (
        CollisionGroup.MidImpassable |
        CollisionGroup.HighImpassable |
        CollisionGroup.LowImpassable);

    /// <summary>
    ///     Collision masks that were removed from ANY layer when the storage was opened;
    /// </summary>
    [DataField("removedMasks")]
    public int RemovedMasks;

    [ViewVariables]
    [DataField("Capacity")]
    public int StorageCapacityMax = 30;

    [ViewVariables]
    [DataField("IsCollidableWhenOpen")]
    public bool IsCollidableWhenOpen;

    [ViewVariables]
    [DataField("EnteringRange")]
    public float EnteringRange = -0.18f;

    [DataField("showContents")]
    public bool ShowContents;

    [DataField("occludesLight")]
    public bool OccludesLight = true;

    [DataField("open")]
    public bool Open;

    [DataField("closeSound")]
    public SoundSpecifier CloseSound = new SoundPathSpecifier("/Audio/Effects/closetclose.ogg");

    [DataField("openSound")]
    public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/Effects/closetopen.ogg");

    [ViewVariables]
    public Container Contents = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsWeldedShut;
}

public sealed class InsertIntoEntityStorageAttemptEvent : CancellableEntityEventArgs { }
public sealed class StoreThisAttemptEvent : CancellableEntityEventArgs { }
public sealed class StorageOpenAttemptEvent : CancellableEntityEventArgs { }
public sealed class StorageCloseAttemptEvent : CancellableEntityEventArgs { }

public sealed class StorageBeforeCloseEvent : EventArgs
{
    public EntityUid Container;

    public HashSet<EntityUid> Contents;

    public HashSet<EntityUid> ContentsWhitelist = new();

    public StorageBeforeCloseEvent(EntityUid container, HashSet<EntityUid> contents)
    {
        Container = container;
        Contents = contents;
    }
}
