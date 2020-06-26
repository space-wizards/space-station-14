using System;
using Content.Server.GameObjects.Components.Weapon.Ranged.Barrels;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Storage;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Storage
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IInteractUsing))]
    public class WeaponStorageComponent : Component, IActivate, IInteractUsing
    {
        protected IEntity _heldItem;
        protected ContainerSlot _container;
        protected AppearanceComponent _appearanceComponent;
        public WallStorageStatus Status => _status;
        protected WallStorageStatus _status;

        public override string Name => "WeaponStorage";

        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var result = TryInsertItem(eventArgs.Using);
            if (!result)
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                eventArgs.User.PopupMessage(Owner, localizationManager.GetString("Unable to insert item"));
            }

            return result;
        }

        public void RemoveItem(IEntity user)
        {
            var heldItem = _container.ContainedEntity;
            if (heldItem == null)
            {
                return;
            }

            _container.Remove(heldItem);
            if (user.TryGetComponent(out HandsComponent handsComponent))
            {
                handsComponent.PutInHandOrDrop(heldItem.GetComponent<ItemComponent>());
            }

            if (heldItem.TryGetComponent(out ServerBatteryBarrelComponent batteryBarrelComponent))
            {
                batteryBarrelComponent.UpdateAppearance();
            }

            UpdateStatus();
        }

        protected abstract WallStorageStatus GetStatus();
        public void UpdateStatus()
        {
            if (_status == status)
            {
                return;
            }

            _status = status;

            switch (_status)
            {
                // Update load just in case
                case WallStorageStatus.Full:
                    _appearanceComponent?.SetData(WallStorageStatus.Full, WallStorageStatus.Full); 
                    break;
                case WallStorageStatus.Empty:
                    _appearanceComponent?.SetData(WallStorageStatus.Empty, WallStorageStatus.Empty); 
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            RemoveItem(eventArgs.User);
        }

        [Verb]
        private sealed class InsertVerb : Verb<WeaponStorageComponent>
        {
            protected override void GetData(IEntity user, WeaponStorageComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (!user.TryGetComponent(out HandsComponent handsComponent))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (handsComponent.GetActiveHand == null)
                {
                    data.Visibility = VerbVisibility.Disabled;
                    data.Text = "Insert";
                    return;
                }

                if (component._container.ContainedEntity != null)
                {
                    data.Visibility = VerbVisibility.Disabled;
                }

                data.Text = $"Insert {handsComponent.GetActiveHand.Owner.Name}";
            }

            protected override void Activate(IEntity user, WeaponStorageComponent component)
            {
                if (!user.TryGetComponent(out HandsComponent handsComponent))
                {
                    return;
                }

                if (handsComponent.GetActiveHand == null)
                {
                    return;
                }
                var userItem = handsComponent.GetActiveHand.Owner;
                handsComponent.Drop(userItem);
                component.TryInsertItem(userItem);
            }
        }

        [Verb]
        private sealed class EjectVerb : Verb<WeaponStorageComponent>
        {
            protected override void GetData(IEntity user, WeaponStorageComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component._container.ContainedEntity == null)
                {
                    data.Visibility = VerbVisibility.Disabled;
                    data.Text = "Eject";
                    return;
                }

                data.Text = $"Eject {component._container.ContainedEntity.Name}";
            }

            protected override void Activate(IEntity user, WeaponStorageComponent component)
            {
                component.RemoveItem(user);
            }
        }

        public bool TryInsertItem(IEntity entity)
        {
            if (!entity.HasComponent<ServerBatteryBarrelComponent>() ||
                _container.ContainedEntity != null)
            {
                return false;
            }

            if (!_container.Insert(entity))
            {
                return false;
            }
            UpdateStatus();
            return true;
        }
    }
}
