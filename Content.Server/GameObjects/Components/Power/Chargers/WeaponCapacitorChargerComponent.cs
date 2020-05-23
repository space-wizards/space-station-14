using System;
using Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
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

namespace Content.Server.GameObjects.Components.Power.Chargers
{
    /// <summary>
    /// This is used for the lasergun / flash rechargers
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IAttackBy))]
    public sealed class WeaponCapacitorChargerComponent : BaseCharger, IActivate, IAttackBy
    {
        public override string Name => "WeaponCapacitorCharger";
        public override double CellChargePercent => _container.ContainedEntity != null ?
            _container.ContainedEntity.GetComponent<HitscanWeaponCapacitorComponent>().Charge /
            _container.ContainedEntity.GetComponent<HitscanWeaponCapacitorComponent>().Capacity * 100 : 0.0f;

        bool IAttackBy.AttackBy(AttackByEventArgs eventArgs)
        {
            var result = TryInsertItem(eventArgs.AttackWith);
            if (!result)
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                eventArgs.User.PopupMessage(Owner, localizationManager.GetString("Unable to insert capacitor"));
            }

            return result;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            RemoveItemToHand(eventArgs.User);
        }

        [Verb]
        private sealed class InsertVerb : Verb<WeaponCapacitorChargerComponent>
        {
            protected override void GetData(IEntity user, WeaponCapacitorChargerComponent component, VerbData data)
            {
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
                component.RemoveItem();
            }
        }

        public bool TryInsertItem(IEntity entity)
        {
            if (!entity.HasComponent<HitscanWeaponCapacitorComponent>() ||
                _container.ContainedEntity != null)
            {
                return false;
            }

            _heldItem = entity;
            if (!_container.Insert(_heldItem))
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

            if (_container.ContainedEntity.TryGetComponent(out HitscanWeaponCapacitorComponent component) &&
                Math.Abs(component.Capacity - component.Charge) < 0.01)
            {
                return CellChargerStatus.Charged;
            }

            return CellChargerStatus.Charging;
        }

        protected override void TransferPower(float frameTime)
        {
            // Two numbers: One for how much power actually goes into the device (chargeAmount) and
            // chargeLoss which is how much is drawn from the powernet
            _container.ContainedEntity.TryGetComponent(out HitscanWeaponCapacitorComponent weaponCapacitorComponent);
            var chargeLoss = weaponCapacitorComponent.RequestCharge(frameTime) * _transferRatio;
            _powerDevice.Load = chargeLoss;

            if (!_powerDevice.Powered)
            {
                // No power: Event should update to Off status
                return;
            }

            var chargeAmount = chargeLoss * _transferEfficiency;

            weaponCapacitorComponent.AddCharge(chargeAmount);
            // Just so the sprite won't be set to 99.99999% visibility
            if (weaponCapacitorComponent.Capacity - weaponCapacitorComponent.Charge < 0.01)
            {
                weaponCapacitorComponent.Charge = weaponCapacitorComponent.Capacity;
            }
            UpdateStatus();
        }

    }
}
