using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

[NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true)]
[RegisterComponent]
public sealed partial class GasMinerComponent : Component
{
    /// <summary>
    /// Operational state of the miner.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public GasMinerState MinerState = GasMinerState.Disabled;

    /// <summary>
    /// If the number of moles in the external environment exceeds this number, no gas will be released.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float MaxExternalAmount = float.PositiveInfinity;

    /// <summary>
    /// If the pressure (in kPA) of the external environment exceeds this number, no gas will be released.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float MaxExternalPressure = Atmospherics.GasMinerDefaultMaxExternalPressure;

    /// <summary>
    /// Gas to spawn.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public Gas SpawnGas;

    /// <summary>
    /// Temperature in Kelvin.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float SpawnTemperature = Atmospherics.T20C;

    /// <summary>
    /// The number of moles released from the miner's internal storage, per second, when the miner is working,
    /// if it has enough gas stored to do so.
    /// </summary>
    [DataField("releaseAmount")]
    public float ReleaseRate = Atmospherics.MolesCellStandard * 20f;

    /// <summary>
    /// The maximum number of moles that can be stored within the miner's internal storage at once.
    /// </summary>
    /// <remarks>
    /// This should not be infinite. Instead, <see cref="ReleaseRate"/> and <see cref="MiningRate"/>
    /// should both be of a lower or equal value compared to this.
    /// </remarks>
    [DataField]
    public float MaxStoredAmount = 1200f;

    /// <summary>
    /// The number of moles that are currently stored within the miner's internal storage, to be released later at the rate of <see cref="ReleaseRate">.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StoredAmount;

    /// <summary>
    /// The <see cref="StoredAmount"/>, the last time which it was replicated. This is used so that continuous very small changes in <see cref="StoredAmount"/> being
    /// intentionally not replicated, will not adversly affect anyone who examines this.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float LastReplicatedStoredAmount;

    /// <summary>
    /// The number of moles that are mined, per second, into the miner's internal storage, not released.
    /// </summary>
    [DataField("spawnAmount")]
    public float MiningRate = Atmospherics.MolesCellStandard * 20f;
}

[Serializable, NetSerializable]
public enum GasMinerState : byte
{
    Disabled,
    Idle,
    Working,
}
