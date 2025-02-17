// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Sith.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSithSystem))]
public sealed partial class SithComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "SithFaction";

    [DataField, AutoNetworkedField]
    public Color EyeColor = new(1.0f, 1.0f, 0.0f);

    [DataField, AutoNetworkedField]
    public Color OldEyeColor = new(1.0f, 1.0f, 0.0f);
}
