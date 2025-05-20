using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Starlight.Avali.Systems;

namespace Content.Shared.Starlight.Avali.Components;

/// <summary>
/// </summary>
/// <remarks>
/// </remarks>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAvaliStasisSystem)), AutoGenerateComponentState]
public sealed partial class AvaliStasisComponent : Component
{
    /// <summary>
    /// Whether the entity is currently in stasis.
    /// </summary>
    [DataField] [AutoNetworkedField] public bool IsInStasis = false;

    /// <summary>
    /// The second entity needed to preform stasis. This is used to leave stasis.
    /// </summary>
    [DataField(required: true)] [ViewVariables(VVAccess.ReadWrite)] [AutoNetworkedField]
    public EntProtoId ExitStasisAction;

    [AutoNetworkedField] [DataField("exitStasisActionEntity")]
    public EntityUid? ExitStasisActionEntity;

    /// <summary>
    /// The entity needed to actually preform stasis. This will be granted (and removed) upon the entity's creation.
    /// </summary>
    [DataField(required: true)] [ViewVariables(VVAccess.ReadWrite)] [AutoNetworkedField]
    public EntProtoId EnterStasisAction;

    [AutoNetworkedField] [DataField("enterStasisActionEntity")]
    public EntityUid? EnterStasisActionEntity;

    /// <summary>
    /// The cooldown time for the stasis ability, in seconds.
    /// </summary>
    [DataField] public float StasisCooldown;

    /// <summary>
    /// The amount of brute damage the stasis ability will heal, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisBluntHealPerSecond;

    /// <summary>
    /// The amount of sharp damage the stasis ability will heal, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisSlashingHealPerSecond;

    /// <summary>
    /// The amount of piercing damage the stasis ability will heal, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisPiercingHealPerSecond;

    /// <summary>
    /// The amount of heat damage the stasis ability will heal, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisHeatHealPerSecond;

    /// <summary>
    /// The amount of cold damage the stasis ability will heal, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisColdHealPerSecond;
    
    /// <summary>
    /// The amount of bleed the stasis ability will heal, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisBleedHealPerSecond;

    /// <summary>
    /// The amount of additional damage resistance while in stasis (0-1, where 1 is 100% resistance), so 0.1 resistance lowers damage by 10%.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisAdditionalDamageResistance;
    
    /// <summary>
    /// The amount of brute damage the stasis ability will heal in critical status, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisInCritBluntHealPerSecond;

    /// <summary>
    /// The amount of sharp damage the stasis ability will heal in critical status, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisInCritSlashingHealPerSecond;

    /// <summary>
    /// The amount of piercing damage the stasis ability will heal in critical status, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisInCritPiercingHealPerSecond;

    /// <summary>
    /// The amount of heat damage the stasis ability will heal in critical status, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisInCritHeatHealPerSecond;

    /// <summary>
    /// The amount of cold damage the stasis ability will heal in critical status, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisInCritColdHealPerSecond;
    
    /// <summary>
    /// The amount of bleed the stasis ability will heal in critical status, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisInCritBleedHealPerSecond;

    /// <summary>
    /// The amount of additional damage resistance while in stasis in critical status (0-1, where 1 is 100% resistance), so 0.1 resistance lowers damage by 10%.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisInCritAdditionalDamageResistance;

    /// <summary>
    /// The prototype ID of the stasis effect to spawn when entering stasis.
    /// </summary>
    [DataField] [AutoNetworkedField] public EntProtoId StasisEnterEffect;

    /// <summary>
    /// The lifetime of the entering stasis effect in seconds.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisEnterEffectLifetime;

    /// <summary>
    /// The sound to play when entering stasis.
    /// </summary>
    [DataField] [AutoNetworkedField] public string StasisEnterSound;
    
    /// <summary>
    /// The prototype ID of the stasis effect to spawn when exiting stasis.
    /// </summary>
    [DataField] [AutoNetworkedField] public EntProtoId StasisExitEffect;

    /// <summary>
    /// The lifetime of the exit stasis effect in seconds.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisExitEffectLifetime;

    /// <summary>
    /// The sound to play when exiting stasis.
    /// </summary>
    [DataField] [AutoNetworkedField] public string StasisExitSound;
    
    /// <summary>
    /// The prototype ID of the stasis effect to spawn when stasis is currently in use.
    /// </summary>
    [DataField] [AutoNetworkedField] public EntProtoId StasisContinuousEffect;

    /// <summary>
    /// The entity reference for the continuous stasis effect.
    /// </summary>
    [DataField] [AutoNetworkedField] public EntityUid? ContinuousEffectEntity;
}