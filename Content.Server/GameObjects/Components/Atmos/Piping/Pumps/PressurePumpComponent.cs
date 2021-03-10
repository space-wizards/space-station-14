#nullable enable
using System;
using Content.Server.Atmos;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Pumps
{
    [RegisterComponent]
    [ComponentReference(typeof(BasePumpComponent))]
    [ComponentReference(typeof(IActivate))]
    public class PressurePumpComponent : BasePumpComponent
    {
        public override string Name => "PressurePump";

        /// <summary>
        ///     The pressure this pump will try to bring its oulet too.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int PressurePumpTarget
        {
            get => _pressurePumpTarget;
            set => _pressurePumpTarget = Math.Clamp(value, 0, MaxPressurePumpTarget);
        }

        [DataField("startingPressurePumpTarget")]
        private int _pressurePumpTarget;

        /// <summary>
        ///     Max value <see cref="PressurePumpTarget"/> can be set to.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxPressurePumpTarget
        {
            get => _maxPressurePumpTarget;
            set => Math.Max(value, 0);
        }
        [DataField("maxPressurePumpTarget")]
        private int _maxPressurePumpTarget = 100;

        /// <summary>
        ///     Every update, this pump will only increase the outlet pressure by this fraction of the amount needed to reach the <see cref="PressurePumpTarget"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferRatio
        {
            get => _transferRatio;
            set => _transferRatio = Math.Clamp(value, 0, 1);
        }
        [DataField("transferRatio")]
        private float _transferRatio = 0.5f;

        protected override void PumpGas(GasMixture inletGas, GasMixture outletGas)
        {
            var goalDiff = PressurePumpTarget - outletGas.Pressure;
            var realGoalPressureDiff = goalDiff * TransferRatio;
            var realTargetPressure = outletGas.Pressure + realGoalPressureDiff;
            var realCappedTargetPressure = Math.Max(realTargetPressure, outletGas.Pressure); //no lowering the outlet's pressure
            inletGas.PumpGasTo(outletGas, realCappedTargetPressure);
        }
    }
}
