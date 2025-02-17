// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Sith.Components;

[RegisterComponent]
public sealed partial class SithLightningAbilityComponent : Component
{
    [DataField("proto")]
    public string LightingPrototypeId = "SithLightning";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Range = 3f;

    [DataField]
    public EntProtoId ActionSithLightning = "ActionSithLightning";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionSithLightningEntity;
}
