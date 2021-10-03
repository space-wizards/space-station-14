using Content.Server.Access.Components;
using Content.Server.Storage.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Lock
{
    /// <summary>
    /// Handles (un)locking and examining of Lock components
    /// </summary>
    [UsedImplicitly]
    public class LockSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LockComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<LockComponent, ActivateInWorldEvent>(OnActivated);
            SubscribeLocalEvent<LockComponent, ExaminedEvent>(OnExamined);
        }

        private void OnStartup(EntityUid uid, LockComponent lockComp, ComponentStartup args)
        {
            if (lockComp.Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(StorageVisuals.CanLock, true);
            }
        }

        private void OnActivated(EntityUid uid, LockComponent lockComp, ActivateInWorldEvent args)
        {
            // Only attempt an unlock by default on Activate
            if (lockComp.Locked)
            {
                DoUnlock(uid, args.User, lockComp);
            }
            else if (lockComp.LockOnClick)
            {
                DoLock(uid, args.User, lockComp);
            }
        }

        private void OnExamined(EntityUid uid, LockComponent lockComp, ExaminedEvent args)
        {
            args.PushText(Loc.GetString(lockComp.Locked
                    ? "lock-comp-on-examined-is-locked"
                    : "lock-comp-on-examined-is-unlocked",
                ("entityName", lockComp.Owner.Name)));
        }

        public bool DoLock(EntityUid uid, IEntity user, LockComponent? lockComp = null)
        {
            if (!Resolve(uid, ref lockComp))
                return false;

            if (!HasUserAccess(uid, user))
                return false;

            lockComp.Owner.PopupMessage(user, Loc.GetString("lock-comp-do-lock-success", ("entityName",lockComp.Owner.Name)));
            lockComp.Locked = true;
            if(lockComp.LockSound != null)
            {
                SoundSystem.Play(Filter.Pvs(lockComp.Owner), lockComp.LockSound.GetSound(), lockComp.Owner, AudioParams.Default.WithVolume(-5));
            }

            if (lockComp.Owner.TryGetComponent(out AppearanceComponent? appearanceComp))
            {
                appearanceComp.SetData(StorageVisuals.Locked, true);
            }

            RaiseLocalEvent(lockComp.Owner.Uid, new LockToggledEvent(true));

            return true;
        }

        public bool DoUnlock(EntityUid uid, IEntity user, LockComponent? lockComp = null)
        {
            if (!Resolve(uid, ref lockComp))
                return false;

            if (!HasUserAccess(uid, user))
                return false;

            lockComp.Owner.PopupMessage(user, Loc.GetString("lock-comp-do-unlock-success", ("entityName", lockComp.Owner.Name)));
            lockComp.Locked = false;
            if(lockComp.UnlockSound != null)
            {
                SoundSystem.Play(Filter.Pvs(lockComp.Owner), lockComp.UnlockSound.GetSound(), lockComp.Owner, AudioParams.Default.WithVolume(-5));
            }

            if (lockComp.Owner.TryGetComponent(out AppearanceComponent? appearanceComp))
            {
                appearanceComp.SetData(StorageVisuals.Locked, false);
            }

            RaiseLocalEvent(lockComp.Owner.Uid, new LockToggledEvent(false));

            // To stop EntityStorageComponent from opening right after the container gets unlocked
            return true;
        }

        private bool HasUserAccess(EntityUid uid, IEntity user, AccessReader? reader = null)
        {
            // Not having an AccessComponent means you get free access. woo!
            if (!Resolve(uid, ref reader))
                return true;

            if (!reader.IsAllowed(user))
            {
                reader.Owner.PopupMessage(user, Loc.GetString("lock-comp-has-user-access-fail"));
                return false;
            }


            return true;
        }
    }
}
