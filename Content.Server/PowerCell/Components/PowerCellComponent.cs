using System;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Examine;
using Content.Shared.PowerCell;
using Content.Shared.Rounding;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.PowerCell.Components
{
    /// <summary>
    /// Batteries that can update an <see cref="AppearanceComponent"/> based on their charge percent
    /// and fit into a <see cref="PowerCellSlotComponent"/> of the appropriate size.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(BatteryComponent))]
#pragma warning disable 618
    public class PowerCellComponent : BatteryComponent, IExamine
#pragma warning restore 618
    {
        public override string Name => "PowerCell";
        public const string SolutionName = "powerCell";

        [ViewVariables] public PowerCellSize CellSize => _cellSize;
        [DataField("cellSize")]
        private PowerCellSize _cellSize = PowerCellSize.Small;

        [ViewVariables] public bool IsRigged { get; set; }

        protected override void Initialize()
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

        public override bool TryUseCharge(float chargeToUse)
        {
            if (IsRigged)
            {
                Explode();
                return false;
            }

            return base.TryUseCharge(chargeToUse);
        }

        public override float UseCharge(float toDeduct)
        {
            if (IsRigged)
            {
                Explode();
                return 0;
            }

            return base.UseCharge(toDeduct);
        }

        private void Explode()
        {
            var heavy = (int) Math.Ceiling(Math.Sqrt(CurrentCharge) / 60);
            var light = (int) Math.Ceiling(Math.Sqrt(CurrentCharge) / 30);

            CurrentCharge = 0;
            EntitySystem.Get<ExplosionSystem>().SpawnExplosion(Owner, 0, heavy, light, light*2);
            IoCManager.Resolve<IEntityManager>().DeleteEntity(Owner);
        }

        private void UpdateVisuals()
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(PowerCellVisuals.ChargeLevel, GetLevel(CurrentCharge / MaxCharge));
            }
        }

        private byte GetLevel(float fraction)
        {
            return (byte) ContentHelpers.RoundToNearestLevels(fraction, 1, SharedPowerCell.PowerCellVisualsLevels);
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (inDetailsRange)
            {
                message.AddMarkup(Loc.GetString("power-cell-component-examine-details", ("currentCharge", $"{CurrentCharge / MaxCharge * 100:F0}")));
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
