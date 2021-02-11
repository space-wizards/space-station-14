using Content.Server.GameObjects.Components.Access;
using Content.Shared.GameObjects.Components.Storage;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    [RegisterComponent]
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class SecureEntityStorageComponent : EntityStorageComponent
    {
        public override string Name => "SecureEntityStorage";
        private bool _locked;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Locked
        {
            get => _locked;
            set
            {
                _locked = value;

                if (Owner.TryGetComponent(out AppearanceComponent appearance))
                {
                    appearance.SetData(StorageVisuals.Locked, _locked);
                }
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _locked, "locked", true);
        }

        protected override void Startup()
        {
            base.Startup();

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
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
            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Machines/door_lock_off.ogg", Owner, AudioParams.Default.WithVolume(-5));
        }

        private void DoLock(IEntity user)
        {
            if (!CheckAccess(user)) return;

            Locked = true;
            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Machines/door_lock_on.ogg", Owner, AudioParams.Default.WithVolume(-5));
        }

        private bool CheckAccess(IEntity user)
        {
            if (Owner.TryGetComponent(out AccessReader reader))
            {
                if (!reader.IsAllowed(user))
                {
                    Owner.PopupMessage(user, Loc.GetString("Access denied"));
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
                if (!ActionBlockerSystem.CanInteract(user) || component.Open)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString(component.Locked ? "Unlock" : "Lock");
            }

            protected override void Activate(IEntity user, SecureEntityStorageComponent component)
            {
                component.DoToggleLock(user);
            }
        }
    }
}
