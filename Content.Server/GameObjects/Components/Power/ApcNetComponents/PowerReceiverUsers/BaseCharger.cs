#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Weapon.Ranged.Barrels;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers
{
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IInteractUsing))]
    public abstract class BaseCharger : Component, IActivate, IInteractUsing
    {
        [ViewVariables]
        private BatteryComponent? _heldBattery;

        [ViewVariables]
        private ContainerSlot _container = default!;

        [ViewVariables]
        private CellChargerStatus _status;

        [ViewVariables]
        private int _chargeRate;

        [ViewVariables]
        private float _transferEfficiency;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _chargeRate, "chargeRate", 100);
            serializer.DataField(ref _transferEfficiency, "transferEfficiency", 0.85f);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponent<PowerReceiverComponent>();
            _container = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-powerCellContainer", Owner);
            // Default state in the visualizer is OFF, so when this gets powered on during initialization it will generally show empty
            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                receiver.OnPowerStateChanged += PowerUpdate;
            }
        }

        public override void OnRemove()
        {
            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                receiver.OnPowerStateChanged -= PowerUpdate;
            }

            _heldBattery = null;

            base.OnRemove();
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var result = TryInsertItem(eventArgs.Using);
            if (!result)
            {
                eventArgs.User.PopupMessage(Owner, Loc.GetString("Unable to insert capacitor"));
            }

            return result;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            RemoveItem(eventArgs.User);
        }

        /// <summary>
        /// This will remove the item directly into the user's hand / floor
        /// </summary>
        /// <param name="user"></param>
        private void RemoveItem(IEntity user)
        {
            var heldItem = _container.ContainedEntity;
            if (heldItem == null)
            {
                return;
            }

            _container.Remove(heldItem);
            _heldBattery = null;
            if (user.TryGetComponent(out HandsComponent? handsComponent))
            {
                handsComponent.PutInHandOrDrop(heldItem.GetComponent<ItemComponent>());
            }

            if (heldItem.TryGetComponent(out ServerBatteryBarrelComponent? batteryBarrelComponent))
            {
                batteryBarrelComponent.UpdateAppearance();
            }

            UpdateStatus();
        }

        private void PowerUpdate(object? sender, PowerStateEventArgs eventArgs)
        {
            UpdateStatus();
        }

        [Verb]
        private sealed class InsertVerb : Verb<BaseCharger>
        {
            protected override void GetData(IEntity user, BaseCharger component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }
                if (!user.TryGetComponent(out HandsComponent? handsComponent))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component._container.ContainedEntity != null || handsComponent.GetActiveHand == null)
                {
                    data.Visibility = VerbVisibility.Disabled;
                    data.Text = Loc.GetString("Insert");
                    return;
                }

                var heldItemName = Loc.GetString(handsComponent.GetActiveHand.Owner.Name);

                data.Text = Loc.GetString("Insert {0}", heldItemName);
            }

            protected override void Activate(IEntity user, BaseCharger component)
            {
                if (!user.TryGetComponent(out HandsComponent? handsComponent))
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
        private sealed class EjectVerb : Verb<BaseCharger>
        {
            protected override void GetData(IEntity user, BaseCharger component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }
                if (component._container.ContainedEntity == null)
                {
                    data.Text = Loc.GetString("Eject");
                    data.Visibility = VerbVisibility.Disabled;
                    return;
                }

                var containerItemName = Loc.GetString(component._container.ContainedEntity.Name);

                data.Text = Loc.GetString("Eject {0}", containerItemName);
            }

            protected override void Activate(IEntity user, BaseCharger component)
            {
                component.RemoveItem(user);
            }
        }

        private CellChargerStatus GetStatus()
        {
            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver) &&
                !receiver.Powered)
            {
                return CellChargerStatus.Off;
            }
            if (_container.ContainedEntity == null)
            {
                return CellChargerStatus.Empty;
            }
            if (_heldBattery != null && Math.Abs(_heldBattery.MaxCharge - _heldBattery.CurrentCharge) < 0.01)
            {
                return CellChargerStatus.Charged;
            }
            return CellChargerStatus.Charging;
        }

        private bool TryInsertItem(IEntity entity)
        {
            if (!IsEntityCompatible(entity) || _container.ContainedEntity != null)
            {
                return false;
            }
            if (!_container.Insert(entity))
            {
                return false;
            }
            _heldBattery = GetBatteryFrom(entity);
            UpdateStatus();
            return true;
        }

        /// <summary>
        ///     If the supplied entity should fit into the charger.
        /// </summary>
        protected abstract bool IsEntityCompatible(IEntity entity);

        protected abstract BatteryComponent GetBatteryFrom(IEntity entity);

        private void UpdateStatus()
        {
            // Not called UpdateAppearance just because it messes with the load
            var status = GetStatus();
            if (_status == status ||
                !Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                return;
            }

            _status = status;
            Owner.TryGetComponent(out AppearanceComponent? appearance);

            switch (_status)
            {
                // Update load just in case
                case CellChargerStatus.Off:
                    receiver.Load = 0;
                    appearance?.SetData(CellVisual.Light, CellChargerStatus.Off);
                    break;
                case CellChargerStatus.Empty:
                    receiver.Load = 0;
                    appearance?.SetData(CellVisual.Light, CellChargerStatus.Empty);
                    break;
                case CellChargerStatus.Charging:
                    receiver.Load = (int) (_chargeRate / _transferEfficiency);
                    appearance?.SetData(CellVisual.Light, CellChargerStatus.Charging);
                    break;
                case CellChargerStatus.Charged:
                    receiver.Load = 0;
                    appearance?.SetData(CellVisual.Light, CellChargerStatus.Charged);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            appearance?.SetData(CellVisual.Occupied, _container.ContainedEntity != null);
        }

        public void OnUpdate(float frameTime) //todo: make single system for this
        {
            if (_status == CellChargerStatus.Empty || _status == CellChargerStatus.Charged || _container.ContainedEntity == null)
            {
                return;
            }
            TransferPower(frameTime);
        }

        private void TransferPower(float frameTime)
        {
            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver) &&
                !receiver.Powered)
            {
                return;
            }

            if (_heldBattery == null)
            {
                return;
            }

            _heldBattery.CurrentCharge += _chargeRate * frameTime;
            // Just so the sprite won't be set to 99.99999% visibility
            if (_heldBattery.MaxCharge - _heldBattery.CurrentCharge < 0.01)
            {
                _heldBattery.CurrentCharge = _heldBattery.MaxCharge;
            }
            UpdateStatus();
        }
    }
}
