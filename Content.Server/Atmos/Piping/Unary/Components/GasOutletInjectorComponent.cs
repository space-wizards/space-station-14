using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    [Access(typeof(GasOutletInjectorSystem))]
    public sealed partial class GasOutletInjectorComponent : Component
    {

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     Target volume to transfer. If <see cref="WideNet"/> is enabled, actual transfer rate will be much higher.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferRate
        {
            get => _transferRate;
            set => _transferRate = Math.Clamp(value, 0f, MaxTransferRate);
        }

        private float _transferRate = 50;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxTransferRate")]
        public float MaxTransferRate = Atmospherics.MaxTransferRate;

        [DataField("maxPressure")]
        public float MaxPressure { get; set; } = GasVolumePumpComponent.DefaultHigherThreshold;

        [DataField("inlet")]
        public string InletName { get; set; } = "pipe";
    }
}
