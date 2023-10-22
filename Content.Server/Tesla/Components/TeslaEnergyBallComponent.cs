using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Tesla.Components;

/// <summary>
/// A component that tracks an entity's saturation level from absorbing other creatures by touch, and spawns new entities when the saturation limit is reached.
/// </summary>
[RegisterComponent]
public sealed partial class TeslaEnergyBallComponent : Component
{

    public float AccumulatedFrametime = 0.0f;

    [DataField]
    public float UpdateInterval = 3.0f;

    /// <summary>
    /// The amount of energy this entity contains. Once the limit is reached, the energy will be spent to spawn mini-energy balls
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Energy;

    /// <summary>
    /// The amount of energy an entity must reach in order to zero the energy and create another entity
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float NeedEnergyToSpawn = 100f;

    /// <summary>
    /// how much energy the entity uses per second.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EnergyLoss = 3f;

    /// <summary>
    /// The amount of energy an entity must reach in order to zero the energy and create another entity
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EnergyToDespawn = -300f;
    /// <summary>
    /// Entities that spawn when the energy limit is reached
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<EntityPrototype>? SpawnProto = "TeslaMiniEnergyBall";

    /// <summary>
    /// Entity, spun when tesla gobbles with touch.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<EntityPrototype>? ConsumeEffectProto = "EffectTeslaSparks";

    /// <summary>
    /// Played when energy reaches the lower limit (and entity destroyed)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier SoundCollapse = default!;
}
