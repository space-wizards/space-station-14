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

        private void OnStartup(EntityUid eUI, LockComponent lockComp, ComponentStartup args)
        {
            if (lockComp.Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(StorageVisuals.CanLock, true);
            }
        }

        private void OnActivated(EntityUid eUI, LockComponent lockComp, ActivateInWorldEvent args)
        {
            // Only attempt an unlock by default on Activate
            if (lockComp.Locked)
            {
                DoUnlock(lockComp, args);
            }
            else
            {
                if (lockComp.LockOnClick)
                    DoLock(lockComp, args);
            }
        }

        private void OnExamined(EntityUid eUI, LockComponent lockComp, ExaminedEvent args)
        {
            args.PushText(Loc.GetString(lockComp.Locked
                    ? "lock-comp-on-examined-is-locked"
                    : "lock-comp-on-examined-is-unlocked",
                ("entityName", lockComp.Owner.Name)));
        }

        public void DoLock(LockComponent lockComp, ActivateInWorldEvent args)
        {
            if (!HasUserAccess(lockComp, args.User))
            {
                return;
            }

            lockComp.Owner.PopupMessage(args.User, Loc.GetString("lock-comp-do-lock-success", ("entityName",lockComp.Owner.Name)));
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

            args.Handled = true;
        }

        public void DoUnlock(LockComponent lockComp, ActivateInWorldEvent args )
        {
            if (!HasUserAccess(lockComp, args.User))
            {
                return;
            }

            lockComp.Owner.PopupMessage(args.User, Loc.GetString("lock-comp-do-unlock-success", ("entityName", lockComp.Owner.Name)));
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
            args.Handled = true;
        }

        private static bool HasUserAccess(LockComponent lockComp, IEntity user)
        {
            if (lockComp.Owner.TryGetComponent(out AccessReader? reader))
            {
                if (!reader.IsAllowed(user))
                {
                    lockComp.Owner.PopupMessage(user, Loc.GetString("lock-comp-has-user-access-fail"));
                    return false;
                }
            }

            return true;
        }
    }
}
