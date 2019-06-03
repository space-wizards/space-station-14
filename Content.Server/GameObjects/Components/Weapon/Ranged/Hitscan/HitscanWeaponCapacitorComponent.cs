using System;
using Content.Shared.GameObjects.Components.Power;
using Content.Server.GameObjects.Components.Power;
using Robust.Shared.Serialization;
using Robust.Server.GameObjects;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan
{
    public class HitscanWeaponCapacitorComponent : PowerCellComponent
    {
        private AppearanceComponent _appearance;

        public override string Name => "HitscanWeaponCapacitor";

        public override float Charge
        {
            get => base.Charge;
            set
            {
                base.Charge = value;
                _updateAppearance();
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
        }

        public override void Initialize()
        {
            base.Initialize();

            Charge = Capacity;
            Owner.TryGetComponent(out _appearance);

        }

        public float GetChargeFrom(float toDeduct)
        {
            //Use this function when you want to shoot even though you don't have enough energy for basecost
            ChargeChanged();
            var chargeChangedBy = Math.Min(this.Charge, toDeduct);
            this.DeductCharge(chargeChangedBy);
            _updateAppearance();
            return chargeChangedBy;
        }

        public void FillFrom(PowerStorageComponent battery)
        {
            var capacitorPowerDeficit = this.Capacity - this.Charge;
            if (battery.CanDeductCharge(capacitorPowerDeficit))
            {
                battery.DeductCharge(capacitorPowerDeficit);
                this.AddCharge(capacitorPowerDeficit);
            }
            else
            {
                this.AddCharge(battery.Charge);
                battery.DeductCharge(battery.Charge);
            }
            _updateAppearance();
        }

        private void _updateAppearance()
        {
            _appearance?.SetData(PowerCellVisuals.ChargeLevel, Charge / Capacity);
        }
    }


}
