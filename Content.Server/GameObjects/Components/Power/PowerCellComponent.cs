using Content.Server.GameObjects.Components.Power.Chargers;
using Content.Shared.GameObjects.Components.Power;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    ///     Batteries that have visuals and can be put into <see cref="PowerCellChargerComponent"/>s.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(BatteryComponent))]
    public class PowerCellComponent : BatteryComponent
    {
        public override string Name => "PowerCell";

        private AppearanceComponent _appearance;

        [ViewVariables]
        public CellType CellType => _cellType;
        private CellType _cellType;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _cellType, "cellType", CellType.PlainCell);
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.TryGetComponent(out _appearance);
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
            _appearance?.SetData(PowerCellVisuals.ChargeLevel, CurrentCharge / MaxCharge);
        }
    }

    public enum CellType
    {
        PlainCell, //This is a battery cell entity
        Weapon, //This is an unremovable battery on a weapon entity
    }
}
