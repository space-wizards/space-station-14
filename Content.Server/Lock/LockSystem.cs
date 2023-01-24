using Content.Server.Storage.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Lock
{
    /// <summary>
    /// Handles (un)locking and examining of Lock components
    /// </summary>
    [UsedImplicitly]
    public sealed class LockSystem : EntitySystem
    {
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly SharedPopupSystem _sharedPopupSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

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
            if (EntityManager.TryGetComponent(lockComp.Owner, out AppearanceComponent? appearance))
            {
                _appearanceSystem.SetData(uid, StorageVisuals.CanLock, true, appearance);
            }
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

        private void OnStorageOpenAttempt(EntityUid uid, LockComponent component, StorageOpenAttemptEvent args)
        {
            if (component.Locked)
            {
                if (!args.Silent)
                    _sharedPopupSystem.PopupEntity(Loc.GetString("entity-storage-component-locked-message"), uid);

                args.Cancel();
            }
        }

        private void OnExamined(EntityUid uid, LockComponent lockComp, ExaminedEvent args)
        {
            args.PushText(Loc.GetString(lockComp.Locked
                    ? "lock-comp-on-examined-is-locked"
                    : "lock-comp-on-examined-is-unlocked",
                ("entityName", EntityManager.GetComponent<MetaDataComponent>(lockComp.Owner).EntityName)));
        }

        public bool TryLock(EntityUid uid, EntityUid user, LockComponent? lockComp = null)
        {
            if (!Resolve(uid, ref lockComp))
                return false;

            if (!CanToggleLock(uid, user, quiet: false))
                return false;

            if (!HasUserAccess(uid, user, quiet: false))
                return false;

            _sharedPopupSystem.PopupEntity(Loc.GetString("lock-comp-do-lock-success", ("entityName", EntityManager.GetComponent<MetaDataComponent>(uid).EntityName)), uid, user);
            lockComp.Locked = true;

            _audio.PlayPvs(_audio.GetSound(lockComp.LockSound), uid, AudioParams.Default.WithVolume(-5));

            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearanceComp))
            {
                _appearanceSystem.SetData(uid, StorageVisuals.Locked, true, appearanceComp);
            }

            RaiseLocalEvent(lockComp.Owner, new LockToggledEvent(true), true);

            return true;
        }

        public void Unlock(EntityUid uid, EntityUid? user, LockComponent? lockComp = null)
        {
            if (!Resolve(uid, ref lockComp))
                return;

            if (user is { Valid: true })
            {
                _sharedPopupSystem.PopupEntity(Loc.GetString("lock-comp-do-unlock-success", ("entityName", EntityManager.GetComponent<MetaDataComponent>(uid).EntityName)), uid, user.Value);
            }

            lockComp.Locked = false;

            _audio.PlayPvs(_audio.GetSound(lockComp.UnlockSound), uid, AudioParams.Default.WithVolume(-5));

            if (EntityManager.TryGetComponent(lockComp.Owner, out AppearanceComponent? appearanceComp))
            {
                _appearanceSystem.SetData(uid, StorageVisuals.Locked, false, appearanceComp);
            }

            RaiseLocalEvent(lockComp.Owner, new LockToggledEvent(false), true);
        }

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
        ///     Before locking the entity, check whether it's a locker. If is, prevent it from being locked from the inside or while it is open.
        /// </summary>
        public bool CanToggleLock(EntityUid uid, EntityUid user, bool quiet = true)
        {
            if (!HasComp<SharedHandsComponent>(user))
                return false;

            var ev = new LockToggleAttemptEvent(user, quiet);
            RaiseLocalEvent(uid, ref ev, true);
            return !ev.Cancelled;
        }

        private bool HasUserAccess(EntityUid uid, EntityUid user, AccessReaderComponent? reader = null, bool quiet = true)
        {
            // Not having an AccessComponent means you get free access. woo!
            if (!Resolve(uid, ref reader))
                return true;

            if (!_accessReader.IsAllowed(user, reader))
            {
                if (!quiet)
                    _sharedPopupSystem.PopupEntity(Loc.GetString("lock-comp-has-user-access-fail"), uid, user);
                return false;
            }

            return true;
        }

        private void AddToggleLockVerb(EntityUid uid, LockComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || !CanToggleLock(uid, args.User))
                return;

            AlternativeVerb verb = new();
            verb.Act = component.Locked ?
                () => TryUnlock(uid, args.User, component) :
                () => TryLock(uid, args.User, component);
            verb.Text = Loc.GetString(component.Locked ? "toggle-lock-verb-unlock" : "toggle-lock-verb-lock");
            verb.IconTexture = component.Locked ? "/Textures/Interface/VerbIcons/unlock.svg.192dpi.png" : "/Textures/Interface/VerbIcons/lock.svg.192dpi.png";
            args.Verbs.Add(verb);
        }

        private void OnEmagged(EntityUid uid, LockComponent component, ref GotEmaggedEvent args)
        {
            if (component.Locked)
            {
                _audio.PlayPvs(_audio.GetSound(component.UnlockSound), uid, AudioParams.Default.WithVolume(-5));

                if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearanceComp))
                {
                    _appearanceSystem.SetData(uid, StorageVisuals.Locked, false, appearanceComp);
                }
                EntityManager.RemoveComponent<LockComponent>(uid); //Literally destroys the lock as a tell it was emagged
                args.Handled = true;
            }
        }
    }
}
