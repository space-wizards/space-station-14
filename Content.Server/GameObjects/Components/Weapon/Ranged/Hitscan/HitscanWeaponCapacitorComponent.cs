using System;
using Content.Server.GameObjects.Components.Power;
using SS14.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan
{
    public class HitscanWeaponCapacitorComponent : PowerCellComponent
    {

        public override string Name => "HitscanWeaponCapacitor";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
        }

        public override void Initialize()
        {
            base.Initialize();

            Charge = Capacity;
        }

        public float GetChargeFrom(float toDeduct)
        {
            //Use this function when you want to shoot even though you don't have enough energy for basecost
            ChargeChanged();
            var chargeChangedBy = Math.Min(this.Charge, toDeduct);
            this.DeductCharge(chargeChangedBy);
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
        }
    }


}
