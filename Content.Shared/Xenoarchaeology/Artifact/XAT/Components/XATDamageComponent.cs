using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for an artifact that is activated after a certain amount of damage is dealt.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(XATDamageSystem))]
public sealed partial class XATDamageComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier AccumulatedDamage = new();

    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> TypesNeeded = new();

    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> GroupsNeeded = new();
}
