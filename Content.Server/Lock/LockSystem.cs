using Content.Server.Access.Components;
using Content.Server.Storage.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Notification.Managers;
using Content.Shared.Storage;
using Content.Shared.Verbs;
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
            SubscribeLocalEvent<LockComponent, GetAlternativeVerbsEvent>(AddToggleLockVerb);
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
            if (args.Handled)
                return;

            // Only attempt an unlock by default on Activate
            if (lockComp.Locked)
            {
                args.Handled = TryUnlock(lockComp, args.User);
            }
            else if (lockComp.LockOnClick)
            {
                args.Handled = TryLock(lockComp, args.User);
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

        public bool TryLock(LockComponent lockComp, IEntity user)
        {
            if (!HasUserAccess(lockComp, user))
            {
                return false;
            }

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

        public bool TryUnlock(LockComponent lockComp, IEntity user)
        {
            if (!HasUserAccess(lockComp, user))
            {
                return false;
            }

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

            return true;
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

        private void AddToggleLockVerb(EntityUid uid, LockComponent component, GetAlternativeVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            // Cannot lock if the entity is currently opened.
            if (component.Owner.TryGetComponent(out EntityStorageComponent? entityStorageComponent)
                && entityStorageComponent.Open)
                return;

            Verb verb = new("togglelock");
            verb.Act = component.Locked ?
                () => TryUnlock(component, args.User) :
                () => TryLock(component, args.User);
            verb.Text = Loc.GetString(component.Locked ? "toggle-lock-verb-unlock" : "toggle-lock-verb-lock");
            // TODO VERB ICONS need padlock open/close icons.
            args.Verbs.Add(verb);
        }
    }
}
