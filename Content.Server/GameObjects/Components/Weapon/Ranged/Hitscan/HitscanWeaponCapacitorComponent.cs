using System;
using Content.Server.GameObjects.Components.Power;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan
{
    [RegisterComponent]
    [ComponentReference(typeof(BatteryComponent))]
    public class HitscanWeaponCapacitorComponent : PowerCellComponent
    {
        public override string Name => "HitscanWeaponCapacitor";

        public override void Initialize()
        {
            base.Initialize();
            CurrentCharge = MaxCharge;
        }

        public float GetChargeFrom(float toDeduct)
        {
            var chargeChangedBy = Math.Min(CurrentCharge, toDeduct);
            CurrentCharge -= chargeChangedBy;
            return chargeChangedBy;
        }

        public void FillFrom(BatteryComponent battery)
        {
            var capacitorPowerDeficit = MaxCharge - CurrentCharge;
            if (battery.TryUseCharge(capacitorPowerDeficit))
            {
                CurrentCharge += capacitorPowerDeficit;
            }
            else
            {
                CurrentCharge += battery.CurrentCharge;
                battery.CurrentCharge = 0;
            }
        }
    }
}
