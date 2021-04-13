#nullable enable
using System;
using Content.Server.Explosions;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
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
    public class PowerCellComponent : BatteryComponent, IExamine, ISolutionChange
    {
        public override string Name => "PowerCell";

        [ViewVariables] public PowerCellSize CellSize => _cellSize;
        [DataField("cellSize")]
        private PowerCellSize _cellSize = PowerCellSize.Small;

        [ViewVariables] public bool IsRigged { get; private set; }

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
            Owner.SpawnExplosion(0, heavy, light, light*2);
            Owner.Delete();
        }

        private void UpdateVisuals()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
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
                message.AddMarkup(Loc.GetString($"The charge indicator reads {CurrentCharge / MaxCharge * 100:F0} %."));
            }
        }

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs)
        {
            IsRigged = Owner.TryGetComponent(out SolutionContainerComponent? solution)
                       && solution.Solution.ContainsReagent("Plasma", out var plasma)
                       && plasma >= 5;
        }
    }

    public enum PowerCellSize
    {
        Small,
        Medium,
        Large
    }
}
