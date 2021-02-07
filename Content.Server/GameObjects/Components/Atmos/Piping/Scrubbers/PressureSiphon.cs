#nullable enable
using System;
using Content.Server.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Scrubbers
{
    [RegisterComponent]
    [ComponentReference(typeof(BaseSiphonComponent))]
    public class PressureSiphonComponent : BaseSiphonComponent
    {
        public override string Name => "PressureSiphon";

        /// <summary>
        ///     The pressure this siphon will try to bring its inlet too.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SiphonPressureTarget
        {
            get => _siphonPressureTarget;
            set => _siphonPressureTarget = Math.Max(value, 0);
        }
        private float _siphonPressureTarget;

        /// <summary>
        ///     Every update, this siphon will only decrease the inlet pressure by this fraction of the amount needed to reach the <see cref="SiphonPressureTarget"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferRatio
        {
            get => _transferRatio;
            set => _transferRatio = Math.Clamp(value, 0, 1);
        }
        private float _transferRatio;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _siphonPressureTarget, "startingSiphonPressureTarget", Atmospherics.OneAtmosphere * 2);
            serializer.DataField(ref _transferRatio, "transferRatio", 0.5f);
        }

        protected override void ScrubGas(GasMixture inletGas, GasMixture outletGas)
        {
            var goalDiff = SiphonPressureTarget - inletGas.Pressure;
            var realGoalPressureDiff = goalDiff * TransferRatio;

            var moleRatioToTransfer = 1 - (inletGas.Pressure + realGoalPressureDiff) / inletGas.Pressure;
            moleRatioToTransfer = Math.Clamp(moleRatioToTransfer, 0, 1);

            var transferedGas = inletGas.RemoveRatio(moleRatioToTransfer);
            outletGas.Merge(transferedGas);
        }
    }
}
