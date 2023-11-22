using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Shared.Lock;

/// <summary>
/// Handles (un)locking and examining of Lock components
/// </summary>
[UsedImplicitly]
public sealed class LockSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _sharedPopupSystem = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LockComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LockComponent, ActivateInWorldEvent>(OnActivated);
        SubscribeLocalEvent<LockComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
        SubscribeLocalEvent<LockComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<LockComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleLockVerb);
        SubscribeLocalEvent<LockComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnStartup(EntityUid uid, LockComponent lockComp, ComponentStartup args)
    {
        _appearanceSystem.SetData(uid, StorageVisuals.CanLock, true);
        _appearanceSystem.SetData(uid, StorageVisuals.Locked, lockComp.Locked);
    }

    private void OnActivated(EntityUid uid, LockComponent lockComp, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        // Only attempt an unlock by default on Activate
        if (lockComp.Locked)
        {
            TryUnlock(uid, args.User, lockComp);
            args.Handled = true;
        }
        else if (lockComp.LockOnClick)
        {
            TryLock(uid, args.User, lockComp);
            args.Handled = true;
        }
    }

    private void OnStorageOpenAttempt(EntityUid uid, LockComponent component, ref StorageOpenAttemptEvent args)
    {
        if (!component.Locked)
            return;

        if (!args.Silent)
            _sharedPopupSystem.PopupClient(Loc.GetString("entity-storage-component-locked-message"), uid, args.User);

        args.Cancelled = true;
    }

    private void OnExamined(EntityUid uid, LockComponent lockComp, ExaminedEvent args)
    {
        args.PushText(Loc.GetString(lockComp.Locked
                ? "lock-comp-on-examined-is-locked"
                : "lock-comp-on-examined-is-unlocked",
            ("entityName", Identity.Name(uid, EntityManager))));
    }

    /// <summary>
    /// Attmempts to lock a given entity
    /// </summary>
    /// <param name="uid">The entity with the lock</param>
    /// <param name="user">The person trying to lock it</param>
    /// <param name="lockComp"></param>
    /// <returns>If locking was successful</returns>
    public bool TryLock(EntityUid uid, EntityUid user, LockComponent? lockComp = null)
    {
        if (!Resolve(uid, ref lockComp))
            return false;

        if (!CanToggleLock(uid, user, quiet: false))
            return false;

        if (!HasUserAccess(uid, user, quiet: false))
            return false;

        _sharedPopupSystem.PopupClient(Loc.GetString("lock-comp-do-lock-success",
                ("entityName", Identity.Name(uid, EntityManager))), uid, user);
        _audio.PlayPredicted(lockComp.LockSound, uid, user, AudioParams.Default.WithVolume(-5));

        lockComp.Locked = true;
        _appearanceSystem.SetData(uid, StorageVisuals.Locked, true);
        Dirty(uid, lockComp);

        var ev = new LockToggledEvent(true);
        RaiseLocalEvent(uid, ref ev, true);
        return true;
    }

    /// <summary>
    /// Forces a given entity to be unlocked
    /// </summary>
    /// <param name="uid">The entity with the lock</param>
    /// <param name="user">The person unlocking it. Can be null</param>
    /// <param name="lockComp"></param>
    public void Unlock(EntityUid uid, EntityUid? user, LockComponent? lockComp = null)
    {
        if (!Resolve(uid, ref lockComp))
            return;

        if (user is { Valid: true })
        {
            _sharedPopupSystem.PopupClient(Loc.GetString("lock-comp-do-unlock-success",
                ("entityName", Identity.Name(uid, EntityManager))), uid, user.Value);
        }

        _audio.PlayPredicted(lockComp.UnlockSound, uid, user, AudioParams.Default.WithVolume(-5));

        lockComp.Locked = false;
        _appearanceSystem.SetData(uid, StorageVisuals.Locked, false);
        Dirty(uid, lockComp);

        var ev = new LockToggledEvent(false);
        RaiseLocalEvent(uid, ref ev, true);
    }


    /// <summary>
    /// Attmempts to unlock a given entity
    /// </summary>
    /// <param name="uid">The entity with the lock</param>
    /// <param name="user">The person trying to unlock it</param>
    /// <param name="lockComp"></param>
    /// <returns>If locking was successful</returns>
    public bool TryUnlock(EntityUid uid, EntityUid user, LockComponent? lockComp = null)
    {
        if (!Resolve(uid, ref lockComp))
            return false;

        if (!CanToggleLock(uid, user, quiet: false))
            return false;

        if (!HasUserAccess(uid, user, quiet: false))
            return false;

        Unlock(uid, user, lockComp);
        return true;
    }

    /// <summary>
    /// Raises an event for other components to check whether or not
    /// the entity can be locked in its current state.
    /// </summary>
    public bool CanToggleLock(EntityUid uid, EntityUid user, bool quiet = true)
    {
        if (!HasComp<HandsComponent>(user))
            return false;

        var ev = new LockToggleAttemptEvent(user, quiet);
        RaiseLocalEvent(uid, ref ev, true);
        return !ev.Cancelled;
    }

    private bool HasUserAccess(EntityUid uid, EntityUid user, AccessReaderComponent? reader = null, bool quiet = true)
    {
        // Not having an AccessComponent means you get free access. woo!
        if (!Resolve(uid, ref reader, false))
            return true;

        if (_accessReader.IsAllowed(user, uid, reader))
            return true;

        if (!quiet)
            _sharedPopupSystem.PopupClient(Loc.GetString("lock-comp-has-user-access-fail"), uid, user);
        return false;
    }

    private void AddToggleLockVerb(EntityUid uid, LockComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !CanToggleLock(uid, args.User))
            return;

        AlternativeVerb verb = new()
        {
            Act = component.Locked ?
                () => TryUnlock(uid, args.User, component) :
                () => TryLock(uid, args.User, component),
            Text = Loc.GetString(component.Locked ? "toggle-lock-verb-unlock" : "toggle-lock-verb-lock"),
            Icon = component.Locked ?
                new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/unlock.svg.192dpi.png")) :
                new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/lock.svg.192dpi.png")),
        };
        args.Verbs.Add(verb);
    }

    private void OnEmagged(EntityUid uid, LockComponent component, ref GotEmaggedEvent args)
    {
        if (!component.Locked || !component.BreakOnEmag)
            return;
        _audio.PlayPredicted(component.UnlockSound, uid, null, AudioParams.Default.WithVolume(-5));
        _appearanceSystem.SetData(uid, StorageVisuals.Locked, false);
        RemComp<LockComponent>(uid); //Literally destroys the lock as a tell it was emagged
        args.Handled = true;
    }
}

