using Content.Server.Tesla.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Tesla.Components;

/// <summary>
/// A component that tracks an entity's saturation level from absorbing other creatures by touch, and spawns new entities when the saturation limit is reached.
/// </summary>
[RegisterComponent, Access(typeof(TeslaEnergyBallSystem))]
public sealed partial class TeslaEnergyBallComponent : Component
{
    /// <summary>
    /// how much energy will Tesla get by eating various things. Walls, people, anything.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ConsumeStuffEnergy = 2f;

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
    /// The amount of energy to which the tesla must reach in order to be destroyed.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EnergyToDespawn = -100f;

    /// <summary>
    /// Played when energy reaches the lower limit (and entity destroyed)
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundCollapse;

    /// <summary>
    /// Entities that spawn when the energy limit is reached
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId SpawnProto = "TeslaMiniEnergyBall";

    /// <summary>
    /// Entity, spun when tesla gobbles with touch.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId ConsumeEffectProto = "EffectTeslaSparks";
}
