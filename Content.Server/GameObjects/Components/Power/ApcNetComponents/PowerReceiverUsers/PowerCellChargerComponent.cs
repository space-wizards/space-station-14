using System;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    ///     Accepts an entity with a <see cref="PowerCellComponent"/>, and adds charge to it if receiving powered from a <see cref="PowerReceiverComponent"/>.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IInteractUsing))]
    public class PowerCellChargerComponent : Component, IActivate, IInteractUsing
    {
        public override string Name => "PowerCellCharger";

        [ViewVariables]
        public IEntity HeldItem { get; private set; }

        [ViewVariables]
        private ContainerSlot _container;

        [ViewVariables]
        private PowerReceiverComponent _powerReceiver;

        [ViewVariables]
        private CellChargerStatus _status;

        private AppearanceComponent _appearanceComponent;

        [ViewVariables]
        public int ChargeRate => _chargeRate;
        private int _chargeRate;

        [ViewVariables]
        private float _chargingEfficiency;

        private int ActiveDrawRate => (int) (ChargeRate / _chargingEfficiency);

        [ViewVariables]
        public CellType CompatibleCellType => _compatibleCellType;
        private CellType _compatibleCellType;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _chargeRate, "chargeRate", 100);
            serializer.DataField(ref _compatibleCellType, "compatibleCellType", CellType.PlainCell);
            serializer.DataField(ref _chargingEfficiency, "chargingEfficiency", 1);
        }

        public override void Initialize()
        {
            base.Initialize();
            _powerReceiver = Owner.GetComponent<PowerReceiverComponent>();
            _container = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-powerCellContainer", Owner);
            _appearanceComponent = Owner.GetComponent<AppearanceComponent>();
            // Default state in the visualizer is OFF, so when this gets powered on during initialization it will generally show empty
            _powerReceiver.OnPowerStateChanged += PowerUpdate;
        }

        public override void OnRemove()
        {
            _powerReceiver.OnPowerStateChanged -= PowerUpdate;
            base.OnRemove();
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            RemoveItemToHand(eventArgs.User);
        }

        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var result = TryInsertItem(eventArgs.Using);
            if (!result)
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                eventArgs.User.PopupMessage(Owner, localizationManager.GetString("Unable to insert capacitor"));
            }
            return result;
        }

        [Verb]
        private sealed class InsertVerb : Verb<PowerCellChargerComponent>
        {
            protected override void GetData(IEntity user, PowerCellChargerComponent component, VerbData data)
            {
                if (!user.TryGetComponent(out HandsComponent handsComponent))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component._container.ContainedEntity != null || handsComponent.GetActiveHand == null)
                {
                    data.Visibility = VerbVisibility.Disabled;
                    data.Text = "Insert";
                    return;
                }

                data.Text = $"Insert {handsComponent.GetActiveHand.Owner.Name}";
            }

            protected override void Activate(IEntity user, PowerCellChargerComponent component)
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
        private sealed class EjectVerb : Verb<PowerCellChargerComponent>
        {
            protected override void GetData(IEntity user, PowerCellChargerComponent component, VerbData data)
            {
                if (component._container.ContainedEntity == null)
                {
                    data.Text = "Eject";
                    data.Visibility = VerbVisibility.Disabled;
                    return;
                }

                data.Text = $"Eject {component._container.ContainedEntity.Name}";
            }

            protected override void Activate(IEntity user, PowerCellChargerComponent component)
            {
                component.RemoveItem();
            }
        }

        public bool TryInsertItem(IEntity entity)
        {
            if (!entity.TryGetComponent<PowerCellComponent>(out var powerCell) || _container.ContainedEntity != null)
            {
                return false;
            }

            if (powerCell.CellType != _compatibleCellType)
            {
                return false;
            }

            HeldItem = entity;
            if (!_container.Insert(HeldItem))
            {
                return false;
            }
            UpdateStatus();
            return true;
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

            _container.Remove(HeldItem);
            HeldItem = null;
            UpdateStatus();
        }

        protected void PowerUpdate(object sender, PowerStateEventArgs eventArgs)
        {
            UpdateStatus();
        }

        protected CellChargerStatus GetStatus()
        {
            if (!_powerReceiver.Powered)
            {
                return CellChargerStatus.Off;
            }

            if (_container.ContainedEntity == null)
            {
                return CellChargerStatus.Empty;
            }

            if (_container.ContainedEntity.TryGetComponent(out PowerCellComponent component) &&
                Math.Abs(component.MaxCharge - component.CurrentCharge) < 0.01)
            {
                return CellChargerStatus.Charged;
            }

            return CellChargerStatus.Charging;
        }

        protected void TransferPower()
        {
            _container.ContainedEntity.TryGetComponent(out PowerCellComponent cellComponent);
            cellComponent.CurrentCharge += _chargeRate;
            // Just so the sprite won't be set to 99.99999% visibility
            if (cellComponent.MaxCharge - cellComponent.CurrentCharge < 0.01)
            {
                cellComponent.CurrentCharge = cellComponent.MaxCharge;
            }
            UpdateStatus();
        }

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
                case CellChargerStatus.Off:
                    _powerReceiver.Load = 0;
                    _appearanceComponent?.SetData(CellVisual.Light, CellChargerStatus.Off);
                    break;
                case CellChargerStatus.Empty:
                    _powerReceiver.Load = 0;
                    _appearanceComponent?.SetData(CellVisual.Light, CellChargerStatus.Empty); ;
                    break;
                case CellChargerStatus.Charging:
                    _powerReceiver.Load = ActiveDrawRate;
                    _appearanceComponent?.SetData(CellVisual.Light, CellChargerStatus.Charging);
                    break;
                case CellChargerStatus.Charged:
                    _powerReceiver.Load = 0;
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
                _container.ContainedEntity == null || !_powerReceiver.Powered)
            {
                return;
            }
            TransferPower();
        }
    }
}
