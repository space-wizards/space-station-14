using Content.Server.Atmos;
using Content.Shared.Physics;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Storage.Components;

[RegisterComponent]
public sealed class EntityStorageComponent : Component, IGasMixtureHolder
{
    public readonly float MaxSize = 1.0f; // maximum width or height of an entity allowed inside the storage.
    public const float GasMixVolume = 70f;

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

    [DataField("capacity")]
    public int Capacity = 30;

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
    public readonly CollisionGroup EnteringOffsetCollisionFlags = CollisionGroup.Impassable | CollisionGroup.MidImpassable;

    [DataField("enteringRange")]
    public float EnteringRange = 0.18f;

    [DataField("showContents")]
    public bool ShowContents;

    [DataField("occludesLight")]
    public bool OccludesLight = true;

    [DataField("deleteContentsOnDestruction"), ViewVariables(VVAccess.ReadWrite)]
    public bool DeleteContentsOnDestruction = false;

    /// <summary>
    /// Whether or not the container is sealed and traps air inside of it
    /// </summary>
    [DataField("airtight"), ViewVariables(VVAccess.ReadWrite)]
    public bool Airtight = true;

    [DataField("open")]
    public bool Open;

    [DataField("closeSound")]
    public SoundSpecifier CloseSound = new SoundPathSpecifier("/Audio/Effects/closetclose.ogg");

    [DataField("openSound")]
    public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/Effects/closetopen.ogg");

    /// <summary>
    ///     Whitelist for what entities are allowed to be inserted into this container. If this is not null, the
    ///     standard requirement that the entity must be an item or mob is waived.
    /// </summary>
    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    [ViewVariables]
    public Container Contents = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsWeldedShut;

    /// <summary>
    ///     Gas currently contained in this entity storage.
    ///     None while open. Grabs gas from the atmosphere when closed, and exposes any entities inside to it.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public GasMixture Air { get; set; } = new (GasMixVolume);
}

public sealed class InsertIntoEntityStorageAttemptEvent : CancellableEntityEventArgs { }
public sealed class StoreMobInItemContainerAttemptEvent : CancellableEntityEventArgs
{
    public bool Handled = false;
}
public sealed class StorageOpenAttemptEvent : CancellableEntityEventArgs
{
    public bool Silent = false;

    public StorageOpenAttemptEvent (bool silent = false)
    {
        Silent = silent;
    }
}
public sealed class StorageBeforeOpenEvent : EventArgs { }
public sealed class StorageAfterOpenEvent : EventArgs { }
public sealed class StorageCloseAttemptEvent : CancellableEntityEventArgs { }
public sealed class StorageBeforeCloseEvent : EventArgs
{
    public HashSet<EntityUid> Contents;

    /// <summary>
    ///     Entities that will get inserted, regardless of any insertion or whitelist checks.
    /// </summary>
    public HashSet<EntityUid> BypassChecks = new();

    public StorageBeforeCloseEvent(HashSet<EntityUid> contents)
    {
        Contents = contents;
    }
}
public sealed class StorageAfterCloseEvent : EventArgs { }
