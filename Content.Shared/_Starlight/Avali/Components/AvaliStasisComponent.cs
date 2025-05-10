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
    [DataField] public float StasisCooldown = 1f;

    /// <summary>
    /// The amount of time the stasis ability will last. In seconds.
    /// </summary>
    [DataField] public float StasisDuration = 120f;

    /// <summary>
    /// The amount of brute damage the stasis ability will heal, per second.
    /// </summary>
    [DataField] public float StasisBluntHeal = 10f;

    /// <summary>
    /// The amount of sharp damage the stasis ability will heal, per second.
    /// </summary>
    [DataField] public float StasisSlashingHeal = 10f;

    /// <summary>
    /// The amount of piercing damage the stasis ability will heal, per second.
    /// </summary>
    [DataField] public float StasisPiercingHeal = 10f;

    /// <summary>
    /// The amount of heat damage the stasis ability will heal, per second.
    /// </summary>
    [DataField] public float StasisHeatHeal = 10f;

    /// <summary>
    /// The amount of cold damage the stasis ability will heal, per second.
    /// </summary>
    [DataField] public float StasisColdHeal = 10f;

    /// <summary>
    /// The amount of damage resistance while in stasis (0-1, where 1 is 100% resistance).
    /// </summary>
    [DataField] [AutoNetworkedField] public float StasisDamageResistance = 0.5f;
}