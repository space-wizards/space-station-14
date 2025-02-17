// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Chemistry.Components;

namespace Content.Shared.DeadSpace.Spiders.SpiderLurker.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SpiderLurkerComponent : Component
{
    [DataField]
    public EntProtoId SpiderLurker = "ActionSpiderLurker";

    [DataField, AutoNetworkedField]
    public EntityUid? SpiderLurkerActionEntity;

    [DataField("durationHide", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DurationHide = TimeSpan.FromSeconds(10);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeLeftHide = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsHide = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId SmokePrototype = "Smoke";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Solution Solution = new();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Duration = 10;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public int SpreadAmount;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float MovementBuff = 1.5f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float MovementSpeedMultiplier = 1f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int SmokeRange = 7;

    [DataField("smokeDuration"), ViewVariables(VVAccess.ReadOnly)]
    public float SmokeDuration = 10;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float BloodCost = 20f;

    #region Visualizer
    [DataField("state")]
    public string State = "running";

    [DataField("hideState")]
    public string HideState = "hide";

    #endregion
}
