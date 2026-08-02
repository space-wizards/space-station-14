// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.QueueTerminal;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class QueueTicketComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Number;

    [DataField]
    public EntityUid? Terminal;

    [DataField]
    public EntityUid? TicketOwner;
}
