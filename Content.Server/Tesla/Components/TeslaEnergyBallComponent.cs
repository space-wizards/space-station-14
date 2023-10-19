using Robust.Shared.GameStates;

using Content.Shared.Singularity.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;

namespace Content.Server.Tesla.Components;

/// <summary>
/// A component that makes the associated entity accumulate energy when an associated event horizon consumes things.
/// Energy management is server-side.
/// </summary>
[RegisterComponent]
public sealed partial class TeslaEnergyBallComponent : Component
{

    public float AccumulatedFrametime = 0.0f;

    [DataField]
    public float UpdateInterval = 3.0f;

    /// <summary>
    /// The amount of energy this tesla contains. Once the limit is reached, the energy will be spent to spawn mini-energy balls
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Energy;

    /// <summary>
    /// 
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float NeedEnergyToSpawn = 100f;

    /// <summary>
    /// how much energy the tesla uses per second.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EnergyLoss = 3f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EnergyToDespawn = -1000f;
    /// <summary>
    /// 
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<EntityPrototype>? SpawnProto = "TeslaMiniEnergyBall";
}
