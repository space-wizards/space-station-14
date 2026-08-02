// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.QueueTerminal;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class QueueTerminalComponent : Component
{
    public const int MaxNumber = 999;

    [DataField]
    public int NextNumber = 1;
    [DataField, AutoNetworkedField]
    public int CalledNumber;

    public EntityUid? CalledTicket;

    public Queue<EntityUid> PendingTickets = new();


    public HashSet<EntityUid> IssuedTo = new();


    [DataField]
    public TimeSpan SignalCooldown = TimeSpan.FromSeconds(0.5);

    public TimeSpan NextSignalTime;

    [DataField]
    public EntProtoId TicketPrototype = "QueueTicket";

    [DataField]
    public SoundSpecifier TicketPrintSound = new SoundPathSpecifier("/Audio/_DeadSpace/QueueTerminal/queue_ticket_print.ogg");

    [DataField]
    public SoundSpecifier CallSound = new SoundPathSpecifier("/Audio/_DeadSpace/QueueTerminal/queue_terminal_announce.ogg");

    [DataField]
    public ProtoId<SinkPortPrototype> CallPort = "Trigger";
}
