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

        public override void Initialize()
        {
            base.Initialize();
            Owner.TryGetComponent(out _appearance);
            UpdateVisuals();
        }

        protected override void OnChargeChanged()
        {
            base.OnChargeChanged();
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            _appearance?.SetData(PowerCellVisuals.ChargeLevel, CurrentCharge / MaxCharge);
        }
    }
}
