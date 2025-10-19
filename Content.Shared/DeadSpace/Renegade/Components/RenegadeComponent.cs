// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Renegade.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRenegadeSystem))]
public sealed partial class RenegadeComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "RenegadeFaction";

    [DataField, AutoNetworkedField]
    public Color EyeColor = new(1.0f, 1.0f, 0.0f);

    [DataField, AutoNetworkedField]
    public Color OldEyeColor = new(1.0f, 1.0f, 0.0f);
}
