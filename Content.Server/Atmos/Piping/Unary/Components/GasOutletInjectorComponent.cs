using Content.Server.Atmos.Piping.Unary.EntitySystems;

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
        public float VolumeRate
        {
            get => _volumeRate;
            set => _volumeRate = Math.Clamp(value, 0f, MaxVolumeRate);
        }

        private float _volumeRate = 50;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxVolumeRate")]
        public float MaxVolumeRate = 200f;

        /// <summary>
        ///     As pressure difference approaches this number, the effective volume rate may be smaller than <see
        ///     cref="VolumeRate"/>
        /// </summary>
        [DataField("MaxPressureDifference")]
        public float MaxPressureDifference = 4500;

        [DataField("inlet")]
        public string InletName { get; set; } = "pipe";

        // TODO ATMOS: Inject method.
    }
}
