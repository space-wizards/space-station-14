using Content.Shared.GameObjects.Components.Power;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Batteries that can update an <see cref="AppearanceComponent"/> based on their charge percent
    /// and fit into a <see cref="PowerCellSlotComponent"/> of the appropriate size.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(BatteryComponent))]
    public class PowerCellComponent : BatteryComponent
    {
        public override string Name => "PowerCell";

        [ViewVariables] public PowerCellSize CellSize => _cellSize;
        private PowerCellSize _cellSize = PowerCellSize.Small;

        /// <summary>
        /// False if we shouldn't waste time updating the AppearanceComponent (if eg. the entity doesn't have one).
        /// </summary>
        private bool _updateVisual = true;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _cellSize, "cellSize", PowerCellSize.Small);
            serializer.DataField(ref _updateVisual, "updateVisual", true);
        }

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

    public enum PowerCellSize
    {
        Small,
        Medium,
        Large
    }
}
