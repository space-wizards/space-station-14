using System;
using Content.Server.GameObjects.Components.NewPower;
using Content.Server.GameObjects.Components.Power;
using Content.Shared.GameObjects.Components.Power;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan
{
    [RegisterComponent]
    [ComponentReference(typeof(BatteryComponent))]
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
                UpdateAppearance();
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
        }

        public override void Initialize()
        {
            base.Initialize();

            Charge = MaxCharge;
            Owner.TryGetComponent(out _appearance);

        }

        public float GetChargeFrom(float toDeduct)
        {
            var chargeChangedBy = Math.Min(Charge, toDeduct);
            this.DeductCharge(chargeChangedBy);
            UpdateAppearance();
            return chargeChangedBy;
        }

        public void FillFrom(BatteryComponent battery)
        {
            var capacitorPowerDeficit = MaxCharge - CurrentCharge;
            if (battery.TryUseCharge(capacitorPowerDeficit))
            {
                AddCharge(capacitorPowerDeficit);
            }
            else
            {
                AddCharge(battery.CurrentCharge);
                battery.CurrentCharge = 0;
            }
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            _appearance?.SetData(PowerCellVisuals.ChargeLevel, Charge / MaxCharge);
        }
    }


}
