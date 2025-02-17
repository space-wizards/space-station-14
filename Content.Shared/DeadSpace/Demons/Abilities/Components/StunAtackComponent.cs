// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.DeadSpace.Demons.Abilities.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class StunAttackComponent : Component
{

    [DataField("healingOnBite")]
    [ViewVariables(VVAccess.ReadOnly)]
    public DamageSpecifier HealingOnBite = new()
    {
        DamageDict = new()
        {
            { "Blunt", -30 },
            { "Slash", -30 },
            { "Piercing", -30 }
        }
    };

    [DataField("stunAttack")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsStunAttack = false;

    [DataField("actionStunAttack", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionStunAttack = "ActionStunAttack";

    [DataField("actionStunAttackEntity")]
    public EntityUid? ActionStunAttackEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWrite)]
    public float ParalyzeTime = 5f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWrite)]
    public float LaunchForwardsMultiplier = 1f;
}
