using System;
using Content.Shared.GameObjects.Components.Power;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.Chargers
{
    public abstract class BaseCharger : Component
    {

        protected IEntity _heldItem;
        protected ContainerSlot _container;
        protected PowerDeviceComponent _powerDevice;
        public CellChargerStatus Status => _status;
        protected CellChargerStatus _status;

        protected AppearanceComponent _appearanceComponent;

        public abstract double CellChargePercent { get; }

        // Powered items have their own charge rates, this is just a way to have chargers with different rates as well
        public float TransferRatio => _transferRatio;
        [ViewVariables]
        protected float _transferRatio;

        public float TransferEfficiency => _transferEfficiency;
        [ViewVariables]
        protected float _transferEfficiency;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _transferRatio, "transfer_ratio", 0.1f);
            serializer.DataField(ref _transferEfficiency, "transfer_efficiency", 0.85f);
        }

        public override void Initialize()
        {
            base.Initialize();
            _powerDevice = Owner.GetComponent<PowerDeviceComponent>();
            if (_powerDevice == null)
            {
                var exc = new InvalidOperationException("Chargers requires a PowerDevice to function");
                Logger.FatalS("charger", exc.Message);
                throw exc;
            }
            _container =
                ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-powerCellContainer", Owner);
            _appearanceComponent = Owner.GetComponent<AppearanceComponent>();
            // Default state in the visualizer is OFF, so when this gets powered on during initialization it will generally show empty
            _powerDevice.OnPowerStateChanged += PowerUpdate;
        }

        /// <summary>
        /// This will remove the item directly into the user's hand rather than the floor
        /// </summary>
        /// <param name="user"></param>
        public void RemoveItemToHand(IEntity user)
        {
            var heldItem = _container.ContainedEntity;
            if (heldItem == null)
            {
                return;
            }
            RemoveItem();

            if (user.TryGetComponent(out HandsComponent handsComponent) &&
                heldItem.TryGetComponent(out ItemComponent itemComponent))
            {
                handsComponent.PutInHand(itemComponent);
            }
        }

        /// <summary>
        ///  Will put the charger's item on the floor if available
        /// </summary>
        public void RemoveItem()
        {
            if (_container.ContainedEntity == null)
            {
                return;
            }

            _container.Remove(_heldItem);
            UpdateStatus();
        }

        protected void PowerUpdate(object sender, PowerStateEventArgs eventArgs)
        {
            UpdateStatus();
        }

        protected abstract CellChargerStatus GetStatus();
        protected abstract void TransferPower(float frameTime);

        protected void UpdateStatus()
        {
            // Not called UpdateAppearance just because it messes with the load
            var status = GetStatus();

            if (_status == status)
            {
                return;
            }

            _status = status;

            switch (_status)
            {
                // Update load just in case
                case CellChargerStatus.Off:
                    _powerDevice.Load = 0;
                    _appearanceComponent?.SetData(CellVisual.Light, CellChargerStatus.Off);
                    break;
                case CellChargerStatus.Empty:
                    _powerDevice.Load = 0;
                    _appearanceComponent?.SetData(CellVisual.Light, CellChargerStatus.Empty); ;
                    break;
                case CellChargerStatus.Charging:
                    _appearanceComponent?.SetData(CellVisual.Light, CellChargerStatus.Charging);
                    break;
                case CellChargerStatus.Charged:
                    _powerDevice.Load = 0;
                    _appearanceComponent?.SetData(CellVisual.Light, CellChargerStatus.Charged);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _appearanceComponent?.SetData(CellVisual.Occupied, _container.ContainedEntity != null);

            _status = status;
        }

        public void OnUpdate(float frameTime)
        {
            if (_status == CellChargerStatus.Empty || _status == CellChargerStatus.Charged ||
                _container.ContainedEntity == null)
            {
                return;
            }

            TransferPower(frameTime);
        }

    }
}
