using Content.Shared.Atmos;
using Robust.Shared.Audio;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public sealed partial class GasCanisterComponent : Component, IGasMixtureHolder
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("port")]
        public string PortName { get; set; } = "port";

        /// <summary>
        ///     Container name for the gas tank holder.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("container")]
        public string ContainerName { get; set; } = "GasCanisterTankHolder";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("gasMixture")]
        public GasMixture Air { get; set; } = new();

        /// <summary>
        ///     Last recorded pressure, for appearance-updating purposes.
        /// </summary>
        public float LastPressure { get; set; } = 0f;

        /// <summary>
        ///     Minimum release pressure possible for the release valve.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("minReleasePressure")]
        public float MinReleasePressure { get; set; } = Atmospherics.OneAtmosphere / 10;

        /// <summary>
        ///     Maximum release pressure possible for the release valve.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxReleasePressure")]
        public float MaxReleasePressure { get; set; } = Atmospherics.OneAtmosphere * 10;

        /// <summary>
        ///     Valve release pressure.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("releasePressure")]
        public float ReleasePressure { get; set; } = Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Whether the release valve is open on the canister.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("releaseValve")]
        public bool ReleaseValve { get; set; } = false;

        [DataField("accessDeniedSound")]
        public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
    }
}
