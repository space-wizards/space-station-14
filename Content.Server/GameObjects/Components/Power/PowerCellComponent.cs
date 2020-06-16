using Content.Shared.GameObjects.Components.Power;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Power
{
    [RegisterComponent]
    [ComponentReference(typeof(BatteryComponent))]
    public class PowerCellComponent : BatteryComponent
    {
        public override string Name => "PowerCell";

        private AppearanceComponent _appearance;

        public virtual float Charge
        {
            get => CurrentCharge;
            set
            {
                CurrentCharge = value;
                UpdateAppearance();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.TryGetComponent(out _appearance);
        }

        public void DeductCharge(float toDeduct)
        {
            CurrentCharge -= toDeduct;
            UpdateAppearance();
        }

        public void AddCharge(float charge)
        {
            CurrentCharge += charge;
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            _appearance?.SetData(PowerCellVisuals.ChargeLevel, Charge / MaxCharge);
        }
    }
}
