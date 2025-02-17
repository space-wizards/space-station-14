// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Abilities.Bloodsucker.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NeededBloodForAutoInjectComponent : Component
{
    [DataField("cost")]
    public float Cost = 30f;
}
