using System.Numerics;
using Content.Shared.Physics;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage.Components;

[NetworkedComponent]
public abstract partial class SharedEntityStorageComponent : Component
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

    /// <summary>
    /// The total amount of items that can fit in one entitystorage
    /// </summary>
    [DataField("capacity")]
    public int Capacity = 30;

    /// <summary>
    /// Whether or not the entity still has collision when open
    /// </summary>
    [DataField("isCollidableWhenOpen")]
    public bool IsCollidableWhenOpen;

    /// <summary>
    /// If true, it opens the storage when the entity inside of it moves
    /// If false, it prevents the storage from opening when the entity inside of it moves.
    /// This is for objects that you want the player to move while inside, like large cardboard boxes, without opening the storage.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("openOnMove")]
    public bool OpenOnMove = true;

    //The offset for where items are emptied/vacuumed for the EntityStorage.
    [DataField("enteringOffset")]
    public Vector2 EnteringOffset = new(0, 0);

    //The collision groups checked, so that items are depositied or grabbed from inside walls.
    [DataField("enteringOffsetCollisionFlags")]
    public CollisionGroup EnteringOffsetCollisionFlags = CollisionGroup.Impassable | CollisionGroup.MidImpassable;

    /// <summary>
    /// How close you have to be to the "entering" spot to be able to enter
    /// </summary>
    [DataField("enteringRange")]
    public float EnteringRange = 0.18f;

    /// <summary>
    /// Whether or not to show the contents when the storage is closed
    /// </summary>
    [DataField("showContents")]
    public bool ShowContents;

    /// <summary>
    /// Whether or not light is occluded by the storage
    /// </summary>
    [DataField("occludesLight")]
    public bool OccludesLight = true;

    /// <summary>
    /// Whether or not all the contents stored should be deleted with the entitystorage
    /// </summary>
    [DataField("deleteContentsOnDestruction"), ViewVariables(VVAccess.ReadWrite)]
    public bool DeleteContentsOnDestruction;

    /// <summary>
    /// Whether or not the container is sealed and traps air inside of it
    /// </summary>
    [DataField("airtight"), ViewVariables(VVAccess.ReadWrite)]
    public bool Airtight = true;

    /// <summary>
    /// Whether or not the entitystorage is open or closed
    /// </summary>
    [DataField("open")]
    public bool Open;

    /// <summary>
    /// The sound made when closed
    /// </summary>
    [DataField("closeSound")]
    public SoundSpecifier CloseSound = new SoundPathSpecifier("/Audio/Effects/closetclose.ogg");

    /// <summary>
    /// The sound made when open
    /// </summary>
    [DataField("openSound")]
    public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/Effects/closetopen.ogg");

    /// <summary>
    ///     Whitelist for what entities are allowed to be inserted into this container. If this is not null, the
    ///     standard requirement that the entity must be an item or mob is waived.
    /// </summary>
    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// The contents of the storage
    /// </summary>
    [ViewVariables]
    public Container Contents = default!;
}

[Serializable, NetSerializable]
public sealed class EntityStorageComponentState : ComponentState
{
    public bool Open;

    public int Capacity;

    public bool IsCollidableWhenOpen;

    public bool OpenOnMove;

    public float EnteringRange;

    public EntityStorageComponentState(bool open, int capacity, bool isCollidableWhenOpen, bool openOnMove, float enteringRange)
    {
        Open = open;
        Capacity = capacity;
        IsCollidableWhenOpen = isCollidableWhenOpen;
        OpenOnMove = openOnMove;
        EnteringRange = enteringRange;
    }
}

[ByRefEvent]
public record struct InsertIntoEntityStorageAttemptEvent(bool Cancelled = false);

[ByRefEvent]
public record struct StoreMobInItemContainerAttemptEvent(bool Handled, bool Cancelled = false);

[ByRefEvent]
public record struct StorageOpenAttemptEvent(EntityUid User, bool Silent, bool Cancelled = false);

[ByRefEvent]
public readonly record struct StorageBeforeOpenEvent;

[ByRefEvent]
public readonly record struct StorageAfterOpenEvent;

[ByRefEvent]
public record struct StorageCloseAttemptEvent(bool Cancelled = false);

[ByRefEvent]
public readonly record struct StorageBeforeCloseEvent(HashSet<EntityUid> Contents, HashSet<EntityUid> BypassChecks);

[ByRefEvent]
public readonly record struct StorageAfterCloseEvent;
