using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for an artifact that is activated after a certain amount of damage is dealt.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(XATDamageThresholdReachedSystem))]
public sealed partial class XATDamageThresholdReachedComponent : Component
{
    /// <summary>
    /// Damage, accumulated by artifact so far. Is cleared on node activation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier AccumulatedDamage = new();

    /// <summary>
    /// Damage that is required to activate trigger, grouped by damage type.
    /// Only one damage type is required, amount of damage must exceed set limit.
    /// <see cref="GroupsNeeded"/> is not required to activate trigger if this
    /// requirement is satisfied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> TypesNeeded = new();

    /// <summary>
    /// Damage that is required to activate trigger, grouped by damage group.
    /// Only one damage type is required, amount of damage must exceed set limit.
    /// <see cref="TypesNeeded"/> is not required to activate trigger if this
    /// requirement is satisfied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> GroupsNeeded = new();
}
