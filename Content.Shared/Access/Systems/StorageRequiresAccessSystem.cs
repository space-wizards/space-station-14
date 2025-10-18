using Content.Shared.Popups;
using Content.Shared.Access.Components;
using Content.Shared.Storage;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Access.Systems;
/// <summary>
/// This system checks if the entity has the access required to open a storage container.
/// Currently, it handles trying to open it via the verb, or trying to insert an item into it.
/// IF YOU USE IT, TEST YOUR STUFF and add catches if necessary.
/// </summary>
public sealed class StorageRequiresAccessSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StorageRequiresAccessComponent, StorageInteractUsingAttemptEvent>(OnStorageInsertWithItemAttempt);
        SubscribeLocalEvent<StorageRequiresAccessComponent, StorageOpenUIAttemptEvent>(OnStorageOpenAttempt);
    }

    /// <summary>
    /// Handles opening a storage via the verb.
    /// </summary>
    /// <param name="openedStorage"> The storage entity that is being opened.</param>
    /// <param name="args"> The event that was raised on the entity.</param>
    private void OnStorageOpenAttempt(Entity<StorageRequiresAccessComponent> openedStorage, ref StorageOpenUIAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_access.IsAllowed(args.User, openedStorage))
        {
            args.Cancel();
            _popup.PopupPredicted(Loc.GetString(openedStorage.Comp.PopupMessage), openedStorage, args.User);
            _audio.PlayPredicted(openedStorage.Comp.SoundDeny, openedStorage.Owner, args.User);
        }
    }

    /// <summary>
    /// Handles trying to insert an item into the storage.
    /// </summary>
    /// <param name="insertedStorage"> The storage entity that is being opened.</param>
    /// <param name="args"> The event that was raised on the entity.</param>
    private void OnStorageInsertWithItemAttempt(Entity<StorageRequiresAccessComponent> insertedStorage, ref StorageInteractUsingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_access.IsAllowed(args.User, insertedStorage))
        {
            args.Cancelled = true;
            _popup.PopupClient(Loc.GetString(insertedStorage.Comp.PopupMessage), insertedStorage, args.User);
        }
    }
}
