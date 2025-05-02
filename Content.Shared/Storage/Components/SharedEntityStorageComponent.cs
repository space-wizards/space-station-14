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
    public TimeSpan NextInternalOpenAttempt;

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
    [DataField]
    public int RemovedMasks;

    /// <summary>
    /// The total amount of items that can fit in one entitystorage
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Capacity = 30;

    /// <summary>
    /// Whether or not the entity still has collision when open
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsCollidableWhenOpen;

    /// <summary>
    /// If true, it opens the storage when the entity inside of it moves
    /// If false, it prevents the storage from opening when the entity inside of it moves.
    /// This is for objects that you want the player to move while inside, like large cardboard boxes, without opening the storage.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool OpenOnMove = true;

    //The offset for where items are emptied/vacuumed for the EntityStorage.
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Vector2 EnteringOffset = new(0, 0);

    //The collision groups checked, so that items are depositied or grabbed from inside walls.
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public CollisionGroup EnteringOffsetCollisionFlags = CollisionGroup.Impassable | CollisionGroup.MidImpassable;

    /// <summary>
    /// How close you have to be to the "entering" spot to be able to enter
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EnteringRange = 0.18f;

    /// <summary>
    /// Whether or not to show the contents when the storage is closed
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ShowContents;

    /// <summary>
    /// Whether or not light is occluded by the storage
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool OccludesLight = true;

    /// <summary>
    /// Whether or not all the contents stored should be deleted with the entitystorage
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool DeleteContentsOnDestruction;

    /// <summary>
    /// Whether or not the container is sealed and traps air inside of it
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Airtight = true;

    /// <summary>
    /// Whether or not the entitystorage is open or closed
    /// </summary>
    [DataField]
    public bool Open;

    /// <summary>
    /// The sound made when closed
    /// </summary>
    [DataField]
    public SoundSpecifier CloseSound = new SoundPathSpecifier("/Audio/Effects/closetclose.ogg");

    /// <summary>
    /// The sound made when open
    /// </summary>
    [DataField]
    public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/Effects/closetopen.ogg");

    /// <summary>
    ///     Whitelist for what entities are allowed to be inserted into this container. If this is not null, the
    ///     standard requirement that the entity must be an item or mob is waived.
    /// </summary>
    [DataField]
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

    public TimeSpan NextInternalOpenAttempt;

    public EntityStorageComponentState(bool open, int capacity, bool isCollidableWhenOpen, bool openOnMove, float enteringRange, TimeSpan nextInternalOpenAttempt)
    {
        Open = open;
        Capacity = capacity;
        IsCollidableWhenOpen = isCollidableWhenOpen;
        OpenOnMove = openOnMove;
        EnteringRange = enteringRange;
        NextInternalOpenAttempt = nextInternalOpenAttempt;
    }
}

[ByRefEvent]
public record struct InsertIntoEntityStorageAttemptEvent(EntityUid ItemToInsert, bool Cancelled = false);

[ByRefEvent]
public record struct StorageOpenAttemptEvent(EntityUid User, bool Silent, bool Cancelled = false);

[ByRefEvent]
public readonly record struct StorageBeforeOpenEvent;

[ByRefEvent]
public readonly record struct StorageAfterOpenEvent;

[ByRefEvent]
public record struct StorageCloseAttemptEvent(EntityUid? User, bool Cancelled = false);

[ByRefEvent]
public readonly record struct StorageBeforeCloseEvent(HashSet<EntityUid> Contents, HashSet<EntityUid> BypassChecks);

[ByRefEvent]
public readonly record struct StorageAfterCloseEvent;
