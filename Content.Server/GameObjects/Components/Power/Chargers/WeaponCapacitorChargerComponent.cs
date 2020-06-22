using System;
using Content.Server.GameObjects.Components.Weapon.Ranged.Barrels;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Components.Power.Chargers
{
    /// <summary>
    /// This is used for the lasergun / flash rechargers
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IInteractUsing))]
    public sealed class WeaponCapacitorChargerComponent : BaseCharger, IActivate, IInteractUsing
    {
        public override string Name => "WeaponCapacitorCharger";
        public override double CellChargePercent => _container.ContainedEntity != null ?
            _container.ContainedEntity.GetComponent<ServerBatteryBarrelComponent>().PowerCell.Charge /
            _container.ContainedEntity.GetComponent<ServerBatteryBarrelComponent>().PowerCell.Capacity * 100 : 0.0f;

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

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            RemoveItem(eventArgs.User);
        }

        [Verb]
        private sealed class InsertVerb : Verb<WeaponCapacitorChargerComponent>
        {
            protected override void GetData(IEntity user, WeaponCapacitorChargerComponent component, VerbData data)
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

            protected override void Activate(IEntity user, WeaponCapacitorChargerComponent component)
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
        private sealed class EjectVerb : Verb<WeaponCapacitorChargerComponent>
        {
            protected override void GetData(IEntity user, WeaponCapacitorChargerComponent component, VerbData data)
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

            protected override void Activate(IEntity user, WeaponCapacitorChargerComponent component)
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

        protected override CellChargerStatus GetStatus()
        {
            if (!_powerDevice.Powered)
            {
                return CellChargerStatus.Off;
            }

            if (_container.ContainedEntity == null)
            {
                return CellChargerStatus.Empty;
            }

            if (_container.ContainedEntity.TryGetComponent(out ServerBatteryBarrelComponent component) &&
                Math.Abs(component.PowerCell.Capacity - component.PowerCell.Charge) < 0.01)
            {
                return CellChargerStatus.Charged;
            }

            return CellChargerStatus.Charging;
        }

        protected override void TransferPower(float frameTime)
        {
            // Two numbers: One for how much power actually goes into the device (chargeAmount) and
            // chargeLoss which is how much is drawn from the powernet
            var powerCell = _container.ContainedEntity.GetComponent<ServerBatteryBarrelComponent>().PowerCell;
            var chargeLoss = powerCell.RequestCharge(frameTime) * _transferRatio;
            _powerDevice.Load = chargeLoss;

            if (!_powerDevice.Powered)
            {
                // No power: Event should update to Off status
                return;
            }

            var chargeAmount = chargeLoss * _transferEfficiency;

            powerCell.AddCharge(chargeAmount);
            // Just so the sprite won't be set to 99.99999% visibility
            if (powerCell.Capacity - powerCell.Charge < 0.01)
            {
                powerCell.Charge = powerCell.Capacity;
            }
            UpdateStatus();
        }
    }
}
