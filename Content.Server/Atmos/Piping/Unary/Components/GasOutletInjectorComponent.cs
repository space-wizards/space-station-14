using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    [Friend(typeof(GasOutletInjectorSystem))]
    public sealed class GasOutletInjectorComponent : Component
    {

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Injecting { get; set; } = false;

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
        public float MaxPressure { get; set; } = 2 * Atmospherics.MaxOutputPressure;

        [DataField("inlet")]
        public string InletName { get; set; } = "pipe";
    }
}
