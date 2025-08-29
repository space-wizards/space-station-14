// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Demons.Abilities.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class StunAttackComponent : Component
{

    [DataField, ViewVariables(VVAccess.ReadOnly)]
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
    public bool IsStunAttack = false;

    [DataField]
    public EntProtoId ActionStunAttack = "ActionStunAttack";

    [DataField]
    public EntityUid? ActionStunAttackEntity;

    [DataField, AutoNetworkedField, Access(Other = AccessPermissions.ReadWrite)]
    public float ParalyzeTime = 5f;

    [DataField, AutoNetworkedField, Access(Other = AccessPermissions.ReadWrite)]
    public float LaunchForwardsMultiplier = 1f;
}
