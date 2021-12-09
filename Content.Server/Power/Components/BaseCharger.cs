using System;
using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IInteractUsing))]
    public abstract class BaseCharger : Component, IActivate, IInteractUsing
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables]
        private BatteryComponent? _heldBattery;

        [ViewVariables]
        public ContainerSlot Container = default!;

        public bool HasCell => Container.ContainedEntity != null;

        [ViewVariables]
        private CellChargerStatus _status;

        [ViewVariables]
        [DataField("chargeRate")]
        private int _chargeRate = 100;

        [ViewVariables]
        [DataField("transferEfficiency")]
        private float _transferEfficiency = 0.85f;

        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponent<ApcPowerReceiverComponent>();
            Container = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-powerCellContainer");
            // Default state in the visualizer is OFF, so when this gets powered on during initialization it will generally show empty
        }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
#pragma warning disable 618
            base.HandleMessage(message, component);
#pragma warning restore 618
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    PowerUpdate(powerChanged);
                    break;
            }
        }

        protected override void OnRemove()
        {
            _heldBattery = null;

            base.OnRemove();
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var result = TryInsertItem(eventArgs.Using);
            if (!result)
            {
                eventArgs.User.PopupMessage(Owner, Loc.GetString("base-charger-on-interact-using-fail"));
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
        public void RemoveItem(EntityUid user)
        {
            if (Container.ContainedEntity is not {Valid: true} heldItem)
            {
                return;
            }

            Container.Remove(heldItem);
            _heldBattery = null;
            if (_entMan.TryGetComponent(user, out HandsComponent? handsComponent))
            {
                handsComponent.PutInHandOrDrop(_entMan.GetComponent<ItemComponent>(heldItem));
            }

            if (_entMan.TryGetComponent(heldItem, out ServerBatteryBarrelComponent? batteryBarrelComponent))
            {
                batteryBarrelComponent.UpdateAppearance();
            }

            UpdateStatus();
        }

        private void PowerUpdate(PowerChangedMessage eventArgs)
        {
            UpdateStatus();
        }

        private CellChargerStatus GetStatus()
        {
            if (_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) &&
                !receiver.Powered)
            {
                return CellChargerStatus.Off;
            }
            if (!HasCell)
            {
                return CellChargerStatus.Empty;
            }
            if (_heldBattery != null && Math.Abs(_heldBattery.MaxCharge - _heldBattery.CurrentCharge) < 0.01)
            {
                return CellChargerStatus.Charged;
            }
            return CellChargerStatus.Charging;
        }

        public bool TryInsertItem(EntityUid entity)
        {
            if (!IsEntityCompatible(entity) || HasCell)
            {
                return false;
            }
            if (!Container.Insert(entity))
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
        public abstract bool IsEntityCompatible(EntityUid entity);

        protected abstract BatteryComponent? GetBatteryFrom(EntityUid entity);

        private void UpdateStatus()
        {
            // Not called UpdateAppearance just because it messes with the load
            var status = GetStatus();
            if (_status == status ||
                !_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver))
            {
                return;
            }

            _status = status;
            _entMan.TryGetComponent(Owner, out AppearanceComponent? appearance);

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

            appearance?.SetData(CellVisual.Occupied, HasCell);
        }

        public void OnUpdate(float frameTime) //todo: make single system for this
        {
            if (_status == CellChargerStatus.Empty || _status == CellChargerStatus.Charged || !HasCell)
            {
                return;
            }
            TransferPower(frameTime);
        }

        private void TransferPower(float frameTime)
        {
            if (_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) &&
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
