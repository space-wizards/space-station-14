// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Spiders.SpiderKnight.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SpiderKnightComponent : Component
{
    [DataField]
    public EntProtoId SpiderKnight = "ActionSpiderKnight";

    [DataField, AutoNetworkedField]
    public EntityUid? SpiderKnightActionEntity;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsRunningState = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsDefendState = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsAttackState = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float MovementBuff = 1.3f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float MovementSpeedDebuff = 0.9f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float MovementSpeedMultiplier = 1f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float DamageMultiply = 2f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float GetDamageMultiply = 0.5f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float BloodCost = 2f;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeLeftPay = TimeSpan.Zero;

    #region Visualizer
    [DataField("state")]
    public string State = "running";

    [DataField("defendState")]
    public string DefendState = "defend";

    [DataField("attackState")]
    public string AttackState = "attack";
    #endregion
}
