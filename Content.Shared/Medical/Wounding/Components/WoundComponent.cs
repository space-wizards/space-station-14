using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Wounding.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(WoundSystem))]
public sealed partial class WoundComponent : Component
{
    /// <summary>
    /// This is the body we are attached to, if we are attached to one
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? Body;

    /// <summary>
    /// Root woundable for our parent, this will always be valid
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid RootWoundable;

    /// <summary>
    /// Current parentWoundable, this will always be valid
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid ParentWoundable;

    /// <summary>
    /// The current severity of the wound expressed as a percentage (/100).
    /// This is used to modify multiple values.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Severity = 100;

    /// <summary>
    /// How much severity we should remove from this wound during a healing update.
    /// This is only used if a healableComponent is also present
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 HealAmount = 1;

    /// <summary>
    /// What damage type is this woundable associated with
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public ProtoId<DamageTypePrototype> AppliedDamageType;

    /// <summary>
    /// How much damage has been applied with this woundable
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public FixedPoint2 AppliedDamage;

    /// <summary>
    /// How much integrity damage are we applying, expressed as a percentage (/100) of applied damage.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 MaxIntegrityDamage;

    /// <summary>
    /// How much are we decreasing our woundables health cap, expressed as a percentage (/100) of applied damage
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 MaxHealthDebuff;

    /// <summary>
    /// How much are we decreasing our woundables integrity cap, expressed as a percentage (/100) of applied damage
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 MaxIntegrityDebuff;
    /// <summary>
    /// How much the integrity damage gets applied, modified by severity
    /// </summary>
    public FixedPoint2 IntegrityDamage => Severity * MaxIntegrityDamage / 100;
    /// <summary>
    /// How much the healthcap gets decreased, modified by severity
    /// </summary>
    public FixedPoint2 HealthDebuff => Severity * MaxHealthDebuff / 100;
    /// <summary>
    /// How much the integrity cap gets decreased, modified by severity
    /// </summary>
    public FixedPoint2 IntegrityDebuff => Severity * MaxIntegrityDebuff / 100;
}
