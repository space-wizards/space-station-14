using Content.Server.Access.Components;
using Content.Server.Storage.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Notification.Managers;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Lock
{
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
            if (lockComp.VerbOverride == LockComponentStateVerbOverride.DoLock)
            {
                DoLock(lockComp, args);
                lockComp.VerbOverride = LockComponentStateVerbOverride.None;
                return;
            }
            else if (lockComp.VerbOverride == LockComponentStateVerbOverride.DoUnlock)
            {
                DoUnlock(lockComp, args);
                lockComp.VerbOverride = LockComponentStateVerbOverride.None;
                return;
            }

            // Only attempt an unlock by default on Activate
            if (lockComp.Locked)
            {
                DoUnlock(lockComp, args);
            }
        }

        private void OnExamined(EntityUid eUI, LockComponent lockComp, ExaminedEvent args)
        {
            args.Message.AddText("\n");
            args.Message.AddText(Loc.GetString(lockComp.Locked
                                                   ? "lock-comp-on-examined-is-locked"
                                                   : "lock-comp-on-examined-is-unlocked",
                                               ("entityName", lockComp.Owner.Name)));
        }

        private void DoLock(LockComponent lockComp, ActivateInWorldEvent args)
        {
            if (!HasUserAccess(lockComp, args.User))
            {
                return;
            }

            lockComp.Owner.PopupMessage(args.User, Loc.GetString("lock-comp-do-lock-success", ("entityName",lockComp.Owner.Name)));
            lockComp.Locked = true;
            SoundSystem.Play(Filter.Pvs(lockComp.Owner), lockComp.LockSound, lockComp.Owner, AudioParams.Default.WithVolume(-5));
            args.Handled = true;
            if (lockComp.Owner.TryGetComponent(out AppearanceComponent? appearanceComp))
            {
                appearanceComp.SetData(StorageVisuals.Locked, true);
            }
        }

        private void DoUnlock(LockComponent lockComp, ActivateInWorldEvent args)
        {
            if (!HasUserAccess(lockComp, args.User))
            {
                return;
            }

            lockComp.Owner.PopupMessage(args.User, Loc.GetString("lock-comp-do-unlock-success", ("entityName", lockComp.Owner.Name)));
            lockComp.Locked = false;
            // To stop EntityStorageComponent from opening right after the container gets unlocked
            args.Handled = true;
            SoundSystem.Play(Filter.Pvs(lockComp.Owner), lockComp.UnlockSound, lockComp.Owner, AudioParams.Default.WithVolume(-5));
            if (lockComp.Owner.TryGetComponent(out AppearanceComponent? appearanceComp))
            {
                appearanceComp.SetData(StorageVisuals.Locked, false);
            }
        }

        private bool HasUserAccess(LockComponent lockComp, IEntity user)
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
