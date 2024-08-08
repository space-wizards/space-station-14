using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

[NetworkedComponent]
[AutoGenerateComponentState]
[RegisterComponent]
public sealed partial class GasMinerComponent : Component
{
    /// <summary>
    ///     Operational state of the miner.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public GasMinerState MinerState = GasMinerState.Disabled;

    /// <summary>
    ///      If the number of moles in the external environment exceeds this number, no gas will be mined.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float MaxExternalAmount = float.PositiveInfinity;

    /// <summary>
    ///      If the pressure (in kPA) of the external environment exceeds this number, no gas will be mined.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float MaxExternalPressure = Atmospherics.GasMinerDefaultMaxExternalPressure;

    /// <summary>
    ///     Gas to spawn.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public Gas SpawnGas;

    /// <summary>
    ///     Temperature in Kelvin.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float SpawnTemperature = Atmospherics.T20C;

    /// <summary>
    ///     Number of moles created per second when the miner is working.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float SpawnAmount = Atmospherics.MolesCellStandard * 20f;
}

[Serializable, NetSerializable]
public enum GasMinerState : byte
{
    Disabled,
    Idle,
    Working,
}
