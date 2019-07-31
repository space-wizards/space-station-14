using Content.Shared.GameObjects.Components.Power;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Power
{
    [RegisterComponent]
    [ComponentReference(typeof(PowerStorageComponent))]
    public class PowerCellComponent : PowerStorageComponent
    {
        public override string Name => "PowerCell";

        private AppearanceComponent _appearance;

        public override float Charge
        {
            get => base.Charge;
            set
            {
                base.Charge = value;
                _updateAppearance();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.TryGetComponent(out _appearance);
        }

        public override void DeductCharge(float toDeduct)
        {
            base.DeductCharge(toDeduct);

            _updateAppearance();
            ChargeChanged();
        }

        public override void AddCharge(float charge)
        {
            base.AddCharge(charge);

            _updateAppearance();
            ChargeChanged();
        }

        private void _updateAppearance()
        {
            _appearance?.SetData(PowerCellVisuals.ChargeLevel, Charge / Capacity);
        }
    }
}
