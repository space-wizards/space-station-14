using Content.Server.Access.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Notification.Managers;
using Content.Shared.Sound;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Storage.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class SecureEntityStorageComponent : EntityStorageComponent
    {
        public override string Name => "SecureEntityStorage";
        [DataField("locked")]
        private bool _locked = true;

        [DataField("unlockSound")] private SoundSpecifier _unlockSound = new SoundPathSpecifier("/Audio/Machines/door_lock_off.ogg");
        [DataField("lockSound")] private SoundSpecifier _lockSound = new SoundPathSpecifier("/Audio/Machines/door_lock_on.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Locked
        {
            get => _locked;
            set
            {
                _locked = value;

                if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                {
                    appearance.SetData(StorageVisuals.Locked, _locked);
                }
            }
        }

        protected override void Startup()
        {
            base.Startup();

            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(StorageVisuals.CanLock, true);
            }
        }

        public override void Activate(ActivateEventArgs eventArgs)
        {
            if (Locked)
            {
                DoToggleLock(eventArgs.User);
                return;
            }

            base.Activate(eventArgs);
        }

        public override bool CanOpen(IEntity user, bool silent = false)
        {
            if (Locked)
            {
                Owner.PopupMessage(user, "It's locked!");
                return false;
            }
            return base.CanOpen(user, silent);
        }

        protected override void OpenVerbGetData(IEntity user, EntityStorageComponent component, VerbData data)
        {
            if (Locked)
            {
                data.Visibility = VerbVisibility.Invisible;

                return;
            }

            base.OpenVerbGetData(user, component, data);
        }

        private void DoToggleLock(IEntity user)
        {
            if (Locked)
            {
                DoUnlock(user);
            }
            else
            {
                DoLock(user);
            }
        }

        private void DoUnlock(IEntity user)
        {
            if (!CheckAccess(user)) return;

            Locked = false;
            if(_unlockSound.TryGetSound(out var unlockSound))
                SoundSystem.Play(Filter.Pvs(Owner), unlockSound, Owner, AudioParams.Default.WithVolume(-5));
        }

        private void DoLock(IEntity user)
        {
            if (!CheckAccess(user)) return;

            Locked = true;
            if(_lockSound.TryGetSound(out var lockSound))
                SoundSystem.Play(Filter.Pvs(Owner), lockSound, Owner, AudioParams.Default.WithVolume(-5));
        }

        private bool CheckAccess(IEntity user)
        {
            if (Owner.TryGetComponent(out AccessReader? reader))
            {
                if (!reader.IsAllowed(user))
                {
                    Owner.PopupMessage(user, Loc.GetString("secure-entity-storage-component-not-allowed-message"));
                    return false;
                }
            }

            return true;
        }

        [Verb]
        private sealed class ToggleLockVerb : Verb<SecureEntityStorageComponent>
        {
            protected override void GetData(IEntity user, SecureEntityStorageComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user) || component.Open)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString(component.Locked ? "toggle-lock-verb-unlock" : "toggle-lock-verb-lock");
            }

            protected override void Activate(IEntity user, SecureEntityStorageComponent component)
            {
                component.DoToggleLock(user);
            }
        }
    }
}
