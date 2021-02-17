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
    public class VolumePumpComponent : BasePumpComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public int VolumePumpRate
        {
            get => _volumePumpRate;
            set => _volumePumpRate = Math.Clamp(value, 0, MaxVolumePumpRate);
        }
        [DataField("startingVolumePumpRate")]
        private int _volumePumpRate;

        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxVolumePumpRate
        {
            get => _maxVolumePumpRate;
            set => Math.Max(value, 0);
        }
        [DataField("maxVolumePumpRate")]
        private int _maxVolumePumpRate = 100;

        public override string Name => "VolumePump";

        protected override void PumpGas(GasMixture inletGas, GasMixture outletGas)
        {
            var volumeRatio = Math.Clamp(VolumePumpRate / inletGas.Volume, 0, 1);
            outletGas.Merge(inletGas.RemoveRatio(volumeRatio));
        }
    }
}
