using Content.Shared.GameObjects.Components.Power;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Batteries that can update an <see cref="AppearanceComponent"/> based on their charge percent
    /// and fit into a <see cref="PowerCellSlotComponent"/> of the appropriate size.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(BatteryComponent))]
    public class PowerCellComponent : BatteryComponent, IExamine
    {
        public override string Name => "PowerCell";

        [ViewVariables] public PowerCellSize CellSize => _cellSize;
        private PowerCellSize _cellSize = PowerCellSize.Small;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _cellSize, "cellSize", PowerCellSize.Small);
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

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if(inDetailsRange)
            {
                message.AddMarkup(Loc.GetString($"The charge indicator reads {CurrentCharge / MaxCharge * 100:F0} %."));
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
