#nullable enable
using Content.Server.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Vents
{
    [RegisterComponent]
    [ComponentReference(typeof(BaseVentComponent))]
    public class PressureVentComponent : BaseVentComponent
    {
        public override string Name => "PressureVent";

        /// <summary>
        ///     The pressure this vent will try to bring its oulet to.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float VentPressureTarget
        {
            get => _ventPressureTarget;
            set => _ventPressureTarget = Math.Clamp(value, 0, MaxVentPressureTarget);
        }
        [DataField("startingVentPressureTarget")]
        private float _ventPressureTarget = Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Max value <see cref="VentPressureTarget"/> can be set to.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxVentPressureTarget
        {
            get => _maxVentPressureTarget;
            set => Math.Max(value, 0);
        }
        [DataField("maxVentPressureTarget")]
        private float _maxVentPressureTarget = Atmospherics.OneAtmosphere * 2;

        /// <summary>
        ///     Every update, this vent will only increase the outlet pressure by this fraction of the amount needed to reach the <see cref="VentPressureTarget"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferRatio
        {
            get => _transferRatio;
            set => _transferRatio = Math.Clamp(value, 0, 1);
        }
        [DataField("transferRatio")]
        private float _transferRatio = 0.5f;

        protected override void VentGas(GasMixture inletGas, GasMixture outletGas)
        {
            var goalDiff = VentPressureTarget - outletGas.Pressure;
            var realGoalPressureDiff = goalDiff * TransferRatio;
            var realTargetPressure = outletGas.Pressure + realGoalPressureDiff;
            var realCappedTargetPressure = Math.Max(realTargetPressure, outletGas.Pressure); //no lowering the outlet's pressure
            inletGas.PumpGasTo(outletGas, realCappedTargetPressure);
        }
    }
}
