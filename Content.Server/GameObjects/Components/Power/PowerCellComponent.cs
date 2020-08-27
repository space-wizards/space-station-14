using Content.Shared.GameObjects.Components.Power;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    ///     Batteries that have update an <see cref="AppearanceComponent"/> based on their charge percent.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(BatteryComponent))]
    public class PowerCellComponent : BatteryComponent
    {
        public override string Name => "PowerCell";

        public override void Initialize()
        {
            base.Initialize();
            CurrentCharge = MaxCharge;
            UpdateVisuals();
        }

        protected override void OnChargeChanged()
        {
            base.OnChargeChanged();
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(PowerCellVisuals.ChargeLevel, CurrentCharge / MaxCharge);
            }
        }
    }
}
