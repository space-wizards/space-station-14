using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Actions.Stasis;

/// <summary>
/// Component that allows an entity to enter and exit stasis.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedStasisSystem)), AutoGenerateComponentState]
public sealed partial class StasisComponent : Component
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
    [DataField] public float StasisCooldown = 300f;

    /// <summary>
    /// The amount of brute damage the stasis ability will heal, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisBluntHealPerSecond = 2f;

    /// <summary>
    /// The amount of sharp damage the stasis ability will heal, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisSlashingHealPerSecond = 2f;

    /// <summary>
    /// The amount of piercing damage the stasis ability will heal, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisPiercingHealPerSecond = 2f;

    /// <summary>
    /// The amount of heat damage the stasis ability will heal, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisHeatHealPerSecond = 2f;

    /// <summary>
    /// The amount of cold damage the stasis ability will heal, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisColdHealPerSecond = 2f;

    /// <summary>
    /// The amount of bleed the stasis ability will heal, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisBleedHealPerSecond = 1.0f;

    /// <summary>
    /// The amount of additional damage resistance while in stasis (0-1, where 1 is 100% resistance), so 0.1 resistance lowers damage by 10%.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisAdditionalDamageResistance = 1.5f;

    /// <summary>
    /// The amount of brute damage the stasis ability will heal in critical status, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisInCritBluntHealPerSecond = 1f;

    /// <summary>
    /// The amount of sharp damage the stasis ability will heal in critical status, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisInCritSlashingHealPerSecond = 1f;

    /// <summary>
    /// The amount of piercing damage the stasis ability will heal in critical status, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisInCritPiercingHealPerSecond = 1f;

    /// <summary>
    /// The amount of heat damage the stasis ability will heal in critical status, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisInCritHeatHealPerSecond = 1f;

    /// <summary>
    /// The amount of cold damage the stasis ability will heal in critical status, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisInCritColdHealPerSecond = 1f;

    /// <summary>
    /// The amount of bleed the stasis ability will heal in critical status, per second.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisInCritBleedHealPerSecond = 0.5f;

    /// <summary>
    /// The amount of additional damage resistance while in stasis in critical status (0-1, where 1 is 100% resistance), so 0.1 resistance lowers damage by 10%.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisInCritAdditionalDamageResistance = 1.5f;

    /// <summary>
    /// The prototype ID of the stasis effect to spawn when entering stasis.
    /// </summary>
    [DataField] [AutoNetworkedField] public EntProtoId StasisEnterEffect = "EffectNanitesEnter";

    /// <summary>
    /// The lifetime of the entering stasis effect in seconds.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisEnterEffectLifetime = 2.7f;

    /// <summary>
    /// The sound to play when entering stasis.
    /// </summary>
    [DataField] [AutoNetworkedField] public string StasisEnterSound = "/Audio/_Starlight/Misc/alien_teleport.ogg";

    /// <summary>
    /// The prototype ID of the stasis effect to spawn when exiting stasis.
    /// </summary>
    [DataField] [AutoNetworkedField] public EntProtoId StasisExitEffect = "EffectNanitesExit";

    /// <summary>
    /// The lifetime of the exit stasis effect in seconds.
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisExitEffectLifetime = 2.7f;

    /// <summary>
    /// The sound to play when exiting stasis.
    /// </summary>
    [DataField] [AutoNetworkedField] public string StasisExitSound = "/Audio/_Starlight/Misc/alien_teleport.ogg";

    /// <summary>
    /// The prototype ID of the stasis effect to spawn when stasis is currently in use.
    /// </summary>
    [DataField] [AutoNetworkedField] public EntProtoId StasisContinuousEffect = "EffectNanitesCurrent";

    /// <summary>
    /// The entity reference for the continuous stasis effect.
    /// </summary>
    [DataField] [AutoNetworkedField] public EntityUid? ContinuousEffectEntity;
}
