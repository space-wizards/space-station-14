using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;

namespace Content.Shared._Starlight.Actions.Storage;

/// <summary>
/// Modifies access to internal storage depending on whether the user initiating it is an outsider.
/// </summary>
public abstract class SharedPrivateStorageSystem : EntitySystem
{
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<StorageComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
        SubscribeLocalEvent<PrivateStorageComponent, PrivateStorageDoAfterEvent>(OnDoAfter);
    }

    private void OnStorageOpenAttempt(EntityUid uid, StorageComponent component, ref StorageOpenAttemptEvent args)
    {
        if(!TryComp<PrivateStorageComponent>(uid, out var actionStorage))
            return;

        if (uid == args.User)
            return;

        // Null check for user
        if (!Exists(args.User))
            return;

        args.Cancelled = true;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, actionStorage.AccessDelay,
            new PrivateStorageDoAfterEvent(), uid, target: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            RequireCanInteract = true,
            BlockDuplicate = true,
            CancelDuplicate = true
        };
        
        _popup.PopupEntity(Loc.GetString(actionStorage.AccessPopup, ("user", args.User)), uid, uid, PopupType.Medium);

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(EntityUid uid, PrivateStorageComponent component, DoAfterEvent args)
    {
        if(args.Cancelled)
            return;
        
        if(args.Handled)
            return;

        // Null check for user
        if (!Exists(args.Args.User))
            return;

        if (TryComp<StorageComponent>(uid, out var storageComp))
        {
            _storage.OpenStorageUI(uid, args.Args.User, storageComp, false);
        }

        args.Handled = true;
    }
}