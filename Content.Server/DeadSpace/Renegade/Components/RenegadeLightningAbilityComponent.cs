// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Renegade.Components;

[RegisterComponent]
public sealed partial class RenegadeLightningAbilityComponent : Component
{
    [DataField("proto")]
    public string LightingPrototypeId = "RenegadeLightning";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Range = 3f;

    [DataField]
    public EntProtoId ActionRenegadeLightning = "ActionRenegadeLightning";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionRenegadeLightningEntity;
}
