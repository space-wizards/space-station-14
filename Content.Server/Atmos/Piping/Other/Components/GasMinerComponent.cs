using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Other.Components
{
    [RegisterComponent]
    public sealed partial class GasMinerComponent : Component
    {
        public bool Enabled { get; set; } = true;

        public bool Broken { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxExternalAmount")]
        public float MaxExternalAmount { get; set; } = float.PositiveInfinity;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxExternalPressure")]
        public float MaxExternalPressure { get; set; } = Atmospherics.GasMinerDefaultMaxExternalPressure;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("spawnGas")]
        public Gas? SpawnGas { get; set; } = null;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("spawnTemperature")]
        public float SpawnTemperature { get; set; } = Atmospherics.T20C;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("spawnAmount")]
        public float SpawnAmount { get; set; } = Atmospherics.MolesCellStandard * 20f;
    }
}
