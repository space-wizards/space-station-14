using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Starlight.Storage;

/// <summary>
/// Modifies access to internal storage depending on whether the user initiating it is the owner of the storage.
/// </summary>
public abstract class SharedPrivateStorageSystem : EntitySystem
{
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<PrivateStorageComponent, PrivateStorageDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PrivateStorageComponent, GetVerbsEvent<ActivationVerb>>(AddPrivateStorageVerb);
    }

    private void OnDoAfter(EntityUid uid, PrivateStorageComponent component, DoAfterEvent args)
    {
        if(args.Cancelled)
            return;
        
        if(args.Handled)
            return;

        if (!Exists(args.Args.User))
            return;

        if (TryComp<StorageComponent>(uid, out var storageComp))
        {
            _storage.OpenStorageUI(uid, args.Args.User, storageComp, false);
        }

        args.Handled = true;
    }

    private void AddPrivateStorageVerb(EntityUid uid, PrivateStorageComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<StorageComponent>(uid, out var storageComp))
            return;

        var uiOpen = _ui.IsUiOpen(uid, StorageComponent.StorageUiKey.Key, args.User);

        ActivationVerb verb = new()
        {
            Act = () =>
            {
                if (uiOpen)
                {
                    // Open immediately for the private storage owner
                    _ui.CloseUi(uid, StorageComponent.StorageUiKey.Key, args.User);
                }
                else
                {
                    // Trigger the private storage access with delay
                    StartPrivateStorageAccess(uid, args.User, component);
                }
            }
        };

        if (uiOpen)
        {
            verb.Text = Loc.GetString("comp-storage-verb-close-storage");
            verb.Icon = new SpriteSpecifier.Texture(
                new("/Textures/Interface/VerbIcons/close.svg.192dpi.png"));
        }
        else
        {
            verb.Text = Loc.GetString("comp-storage-verb-open-storage");
            verb.Icon = new SpriteSpecifier.Texture(
                new("/Textures/Interface/VerbIcons/open.svg.192dpi.png"));
        }
        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Starts the private storage access process with delay and popup.
    /// If the user is the owner of the storage, access is granted immediately without delay.
    /// </summary>
    private void StartPrivateStorageAccess(EntityUid uid, EntityUid user, PrivateStorageComponent component)
    {
        // If the user is the owner of the storage, grant immediate access
        if (uid == user)
        {
            if (TryComp<StorageComponent>(uid, out var storageComp))
            {
                _storage.OpenStorageUI(uid, user, storageComp, false);
            }
            return;
        }

        // For other users, require DoAfter with delay
        var doAfterArgs = new DoAfterArgs(EntityManager, user, component.AccessDelay,
            new PrivateStorageDoAfterEvent(), uid, target: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            RequireCanInteract = true,
            BlockDuplicate = true,
            CancelDuplicate = true
        };
        
        _popup.PopupEntity(Loc.GetString(component.AccessPopup, ("user", user)), uid, uid, PopupType.Medium);
        _doAfter.TryStartDoAfter(doAfterArgs);
    }
}
