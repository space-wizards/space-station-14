using Content.Shared.ActionBlocker;
using Content.Shared.Construction.Components;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Content.Shared.Wires;
using Content.Shared.Item.ItemToggle.Components;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Lock;

/// <summary>
/// Handles (un)locking and examining of Lock components
/// </summary>
[UsedImplicitly]
public sealed class LockSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _sharedPopupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private readonly LocId _defaultDenyReason = "lock-comp-has-user-access-fail";

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LockComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LockComponent, ActivateInWorldEvent>(OnActivated, before: [typeof(ActivatableUISystem)]);
        SubscribeLocalEvent<LockComponent, UseInHandEvent>(OnUseInHand, before: [typeof(ActivatableUISystem)]);
        SubscribeLocalEvent<LockComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
        SubscribeLocalEvent<LockComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<LockComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleLockVerb);
        SubscribeLocalEvent<LockComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<LockComponent, LockDoAfter>(OnDoAfterLock);
        SubscribeLocalEvent<LockComponent, UnlockDoAfter>(OnDoAfterUnlock);


        SubscribeLocalEvent<LockedWiresPanelComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);
        SubscribeLocalEvent<LockedWiresPanelComponent, AttemptChangePanelEvent>(OnAttemptChangePanel);
        SubscribeLocalEvent<LockedAnchorableComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<LockedStorageComponent, StorageInteractAttemptEvent>(OnStorageInteractAttempt);

        SubscribeLocalEvent<UIRequiresLockComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<UIRequiresLockComponent, LockToggledEvent>(LockToggled);

        SubscribeLocalEvent<ItemToggleRequiresLockComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
    }

    private void OnStartup(EntityUid uid, LockComponent lockComp, ComponentStartup args)
    {
        _appearanceSystem.SetData(uid, LockVisuals.Locked, lockComp.Locked);
    }

    private void OnActivated(EntityUid uid, LockComponent lockComp, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        // Only attempt an unlock by default on Activate
        if (lockComp.Locked && lockComp.UnlockOnClick)
        {
            args.Handled = true;
            TryUnlock(uid, args.User, lockComp);
        }
        else if (!lockComp.Locked && lockComp.LockOnClick)
        {
            args.Handled = true;
            TryLock(uid, args.User, lockComp);
        }
    }

    private void OnUseInHand(EntityUid uid, LockComponent lockComp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (lockComp.Locked && lockComp.UnlockInHand)
        {
            args.Handled = true;
            TryUnlock(uid, args.User, lockComp);
        }
        else if (!lockComp.Locked && lockComp.LockInHand)
        {
            args.Handled = true;
            TryLock(uid, args.User, lockComp);
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
        if (!lockComp.ShowExamine)
            return;

        args.PushMarkup(Loc.GetString(lockComp.Locked
                ? "lock-comp-on-examined-is-locked"
                : "lock-comp-on-examined-is-unlocked",
            ("entityName", Identity.Name(uid, EntityManager))));
    }

    /// <summary>
    /// Attmempts to lock a given entity
    /// </summary>
    /// <remarks>
    /// If the lock is set to require a do-after, a true return value only indicates that the do-after started.
    /// </remarks>
    /// <param name="uid">The entity with the lock</param>
    /// <param name="user">The person trying to lock it</param>
    /// <param name="lockComp"></param>
    /// <param name="skipDoAfter">If true, skip the required do-after if one is configured.</param>
    /// <returns>If locking was successful</returns>
    public bool TryLock(EntityUid uid, EntityUid user, LockComponent? lockComp = null, bool skipDoAfter = false)
    {
        if (!Resolve(uid, ref lockComp))
            return false;

        if (!CanToggleLock(uid, user, quiet: false))
            return false;

        if (lockComp.UseAccess && !HasUserAccess(uid, user, false))
            return false;

        if (!skipDoAfter && lockComp.LockTime != TimeSpan.Zero)
        {
            return _doAfter.TryStartDoAfter(
                new DoAfterArgs(EntityManager, user, lockComp.LockTime, new LockDoAfter(), uid, uid)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    NeedHand = true,
                    BreakOnDropItem = false,
                });
        }

        Lock(uid, user, lockComp);
        return true;
    }

    /// <summary>
    ///     Forces a given entity to be locked, does not activate a do-after.
    /// </summary>
    public void Lock(EntityUid uid, EntityUid? user, LockComponent? lockComp = null)
    {
        if (!Resolve(uid, ref lockComp))
            return;

        if (lockComp.Locked)
            return;

        if (user is { Valid: true })
        {
            _sharedPopupSystem.PopupClient(Loc.GetString("lock-comp-do-lock-success",
                ("entityName", Identity.Name(uid, EntityManager))), uid, user);
        }

        _audio.PlayPredicted(lockComp.LockSound, uid, user);

        lockComp.Locked = true;
        _appearanceSystem.SetData(uid, LockVisuals.Locked, true);
        Dirty(uid, lockComp);

        var ev = new LockToggledEvent(true);
        RaiseLocalEvent(uid, ref ev, true);
    }

    /// <summary>
    /// Forces a given entity to be unlocked
    /// </summary>
    /// <remarks>
    /// This does not process do-after times.
    /// </remarks>
    /// <param name="uid">The entity with the lock</param>
    /// <param name="user">The person unlocking it. Can be null</param>
    /// <param name="lockComp"></param>
    public void Unlock(EntityUid uid, EntityUid? user, LockComponent? lockComp = null)
    {
        if (!Resolve(uid, ref lockComp))
            return;

        if (!lockComp.Locked)
            return;

        if (user is { Valid: true })
        {
            _sharedPopupSystem.PopupClient(Loc.GetString("lock-comp-do-unlock-success",
                ("entityName", Identity.Name(uid, EntityManager))), uid, user.Value);
        }

        _audio.PlayPredicted(lockComp.UnlockSound, uid, user);

        lockComp.Locked = false;
        _appearanceSystem.SetData(uid, LockVisuals.Locked, false);
        Dirty(uid, lockComp);

        var ev = new LockToggledEvent(false);
        RaiseLocalEvent(uid, ref ev, true);
    }


    /// <summary>
    /// Attmempts to unlock a given entity
    /// </summary>
    /// <remarks>
    /// If the lock is set to require a do-after, a true return value only indicates that the do-after started.
    /// </remarks>
    /// <param name="uid">The entity with the lock</param>
    /// <param name="user">The person trying to unlock it</param>
    /// <param name="lockComp"></param>
    /// <param name="skipDoAfter">If true, skip the required do-after if one is configured.</param>
    /// <returns>If locking was successful</returns>
    public bool TryUnlock(EntityUid uid, EntityUid user, LockComponent? lockComp = null, bool skipDoAfter = false)
    {
        if (!Resolve(uid, ref lockComp))
            return false;

        if (!CanToggleLock(uid, user, quiet: false))
            return false;

        if (lockComp.UseAccess && !HasUserAccess(uid, user, false))
            return false;

        if (!skipDoAfter && lockComp.UnlockTime != TimeSpan.Zero)
        {
            return _doAfter.TryStartDoAfter(
                new DoAfterArgs(EntityManager, user, lockComp.LockTime, new UnlockDoAfter(), uid, uid)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    NeedHand = true,
                    BreakOnDropItem = false,
                });
        }

        Unlock(uid, user, lockComp);
        return true;
    }

    /// <summary>
    /// Toggle the lock to locked if unlocked, and unlocked if locked.
    /// </summary>
    /// <param name="uid">Entity to toggle the lock state of.</param>
    /// <param name="user">The person trying to toggle the lock</param>
    /// <param name="lockComp">Entities lock comp (will be resolved)</param>
    public void ToggleLock(EntityUid uid, EntityUid? user, LockComponent? lockComp = null)
    {
        if (IsLocked((uid, lockComp)))
            Unlock(uid, user, lockComp);
        else
            Lock(uid, user, lockComp);
    }

    /// <summary>
    /// Returns true if the entity is locked.
    /// Entities with no lock component are considered unlocked.
    /// </summary>
    public bool IsLocked(Entity<LockComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return ent.Comp.Locked;
    }

    /// <summary>
    /// Raises an event for other components to check whether or not
    /// the entity can be locked in its current state.
    /// </summary>
    public bool CanToggleLock(Entity<LockComponent?> ent, EntityUid user, bool quiet = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!_actionBlocker.CanComplexInteract(user))
            return false;

        if (!ent.Comp.Locked && !ent.Comp.AllowRepeatedLocking)
            return false;

        var ev = new LockToggleAttemptEvent(user, quiet);
        RaiseLocalEvent(ent, ref ev, true);
        if (ev.Cancelled)
            return false;

        var userEv = new UserLockToggleAttemptEvent(ent, quiet);
        RaiseLocalEvent(user, ref userEv, true);
        return !userEv.Cancelled;
    }

    /// <summary>
    /// Checks whether the user has access to locks on an entity.
    /// </summary>
    /// <param name="ent">The entity we check for locks.</param>
    /// <param name="user">The user we check for access.</param>
    /// <param name="quiet">Whether to display a popup if user has no access.</param>
    /// <returns>True if the user has access, otherwise False.</returns>
    [PublicAPI]
    public bool HasUserAccess(Entity<LockComponent?> ent, EntityUid user, bool quiet = true)
    {
        // Entity literally has no lock. Congratulations.
        if (!Resolve(ent, ref ent.Comp, false))
            return true;

        var checkedReaders = LockTypes.None;
        if (ent.Comp.CheckedLocks is null)
        {
            var lockEv = new FindAvailableLocksEvent(user);
            RaiseLocalEvent(ent, ref lockEv);
            checkedReaders = lockEv.FoundReaders;
        }

        // If no locks are found, you have access. Woo!
        if (checkedReaders == LockTypes.None)
            return true;

        var accessEv = new CheckUserHasLockAccessEvent(user, checkedReaders);
        RaiseLocalEvent(ent, ref accessEv);

        // If we check for any, as long as user has access to any of the locks we grant access.
        if (accessEv.HasAccess != LockTypes.None && ent.Comp.CheckForAnyReaders)
            return true;

        if (accessEv.HasAccess == checkedReaders)
            return true;

        if (!quiet)
        {
            var denyReason = accessEv.DenyReason ?? Loc.GetString(_defaultDenyReason);
            _sharedPopupSystem.PopupClient(denyReason, ent, user);
        }

        return false;
    }

    private void AddToggleLockVerb(EntityUid uid, LockComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || !component.ShowLockVerbs)
            return;

        AlternativeVerb verb = new()
        {
            Disabled = !CanToggleLock(uid, args.User),
            Act = component.Locked
                ? () => TryUnlock(uid, args.User, component)
                : () => TryLock(uid, args.User, component),
            Text = Loc.GetString(component.Locked ? "toggle-lock-verb-unlock" : "toggle-lock-verb-lock"),
            Icon = !component.Locked
                ? new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/lock.svg.192dpi.png"))
                : new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/unlock.svg.192dpi.png")),
        };
        args.Verbs.Add(verb);
    }

    private void OnEmagged(EntityUid uid, LockComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Access))
            return;

        if (!component.Locked || !component.BreakOnAccessBreaker)
            return;

        _audio.PlayPredicted(component.UnlockSound, uid, args.UserUid);

        component.Locked = false;
        _appearanceSystem.SetData(uid, LockVisuals.Locked, false);
        Dirty(uid, component);

        var ev = new LockToggledEvent(false);
        RaiseLocalEvent(uid, ref ev, true);

        args.Repeatable = true;
        args.Handled = true;
    }

    private void OnDoAfterLock(EntityUid uid, LockComponent component, LockDoAfter args)
    {
        if (args.Cancelled)
            return;

        TryLock(uid, args.User, skipDoAfter: true);
    }

    private void OnDoAfterUnlock(EntityUid uid, LockComponent component, UnlockDoAfter args)
    {
        if (args.Cancelled)
            return;

        TryUnlock(uid, args.User, skipDoAfter: true);
    }

    private void OnStorageInteractAttempt(Entity<LockedStorageComponent> ent, ref StorageInteractAttemptEvent args)
    {
        if (IsLocked(ent.Owner))
            args.Cancelled = true;
    }

    private void OnLockToggleAttempt(Entity<LockedWiresPanelComponent> ent, ref LockToggleAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<WiresPanelComponent>(ent, out var panel) || !panel.Open)
            return;

        if (!args.Silent)
        {
            _sharedPopupSystem.PopupClient(Loc.GetString("construction-step-condition-wire-panel-close"),
                ent,
                args.User);
        }
        args.Cancelled = true;
    }


    private void OnAttemptChangePanel(Entity<LockedWiresPanelComponent> ent, ref AttemptChangePanelEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<LockComponent>(ent, out var lockComp) || !lockComp.Locked)
            return;

        _sharedPopupSystem.PopupClient(Loc.GetString("lock-comp-generic-fail",
            ("target", Identity.Entity(ent, EntityManager))),
            ent,
            args.User);
        args.Cancelled = true;
    }

    private void OnUnanchorAttempt(Entity<LockedAnchorableComponent> ent, ref UnanchorAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<LockComponent>(ent, out var lockComp) || !lockComp.Locked)
            return;

        _sharedPopupSystem.PopupClient(Loc.GetString("lock-comp-generic-fail",
                ("target", Identity.Entity(ent, EntityManager))),
            ent,
            args.User);
        args.Cancel();
    }

    private void OnUIOpenAttempt(EntityUid uid, UIRequiresLockComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<LockComponent>(uid, out var lockComp) || lockComp.Locked == component.RequireLocked)
            return;

        args.Cancel();

        if (args.Silent)
            return;

        if (lockComp.Locked && component.Popup != null)
        {
            _sharedPopupSystem.PopupClient(Loc.GetString(component.Popup), uid, args.User);
        }

        _audio.PlayPredicted(component.AccessDeniedSound, uid, args.User);
    }

    private void LockToggled(EntityUid uid, UIRequiresLockComponent component, LockToggledEvent args)
    {
        if (!TryComp<LockComponent>(uid, out var lockComp) || lockComp.Locked == component.RequireLocked)
            return;

        if (component.UserInterfaceKeys == null)
        {
            _ui.CloseUis(uid);
            return;
        }

        foreach (var key in component.UserInterfaceKeys)
        {
            _ui.CloseUi(uid, key);
        }
    }

    private void OnActivateAttempt(EntityUid uid, ItemToggleRequiresLockComponent component, ref ItemToggleActivateAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<LockComponent>(uid, out var lockComp) || lockComp.Locked == component.RequireLocked)
            return;

        args.Cancelled = true;

        if (lockComp.Locked && component.LockedPopup != null)
        {
            _sharedPopupSystem.PopupClient(Loc.GetString(component.LockedPopup,
                    ("target", Identity.Entity(uid, EntityManager))),
                uid,
                args.User);
        }
    }
}

/// <summary>
/// Raised on an entity to check whether it has any readers that can prevent it from being opened.
/// </summary>
/// <param name="User">The person attempting to open the entity.</param>
/// <param name="FoundReaders">What readers were found. This should not be set when raising the event.</param>
[ByRefEvent]
public record struct FindAvailableLocksEvent(EntityUid User, LockTypes FoundReaders = LockTypes.None);

/// <summary>
/// Raised on an entity to check if the user has access (ID, Fingerprint, etc) to said entity.
/// </summary>
/// <param name="User">The user we are checking.</param>
/// <param name="FoundReaders">What readers we are attempting to verify access for.</param>
/// <param name="HasAccess">Which readers the user has access to. This should not be set when raising the event.</param>
[ByRefEvent]
public record struct CheckUserHasLockAccessEvent(EntityUid User, LockTypes FoundReaders = LockTypes.None, LockTypes HasAccess = LockTypes.None, string? DenyReason = null);

/// <summary>
/// Enum of all readers a lock can be "locked" by.
/// Used to determine what you need in order to access the lock.
/// For example, an entity with <see cref="AccessReaderComponent"/> will have the Access type, which is gathered by an event and handled by the respective system.
/// </summary>
[Flags]
[Serializable, NetSerializable]
public enum LockTypes : byte
{
    None, // Default state, means the lock is not restricted.
    Access, // Means there is an AccessReader currently present.
    Fingerprint, // Means there is a FingerprintReader currently present.
    All = Access | Fingerprint,
}
