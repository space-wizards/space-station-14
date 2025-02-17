// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Demons.Demon.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AutoDeleteOnDeathComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string DeadSound = "/Audio/Effects/demon_dies.ogg";
}
