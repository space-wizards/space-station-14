using System;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Afk;
using Content.Server.Database;
using Content.Server.Discord;
using Content.Server.GameTicking;
using Content.Server.Players.RateLimiting;
using Content.Shared.Starlight;
using Content.Shared.Starlight.CCVar;
using Content.Shared.Starlight.MHelp;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mind;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Starlight;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.Ghost;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Server.Administration.Logs;
using Content.Shared._NullLink;
using Content.Server._NullLink.PlayerData;

namespace Content.Server.Administration.Systems;

[UsedImplicitly]
public sealed partial class MentorSystem : SharedMentorSystem
{
    private const string RateLimitKey = "MentorHelp";

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPlayerRolesManager _playerRolesManager = default!;
    [Dependency] private readonly ISharedNullLinkPlayerRolesReqManager _playerRoles = default!;
    [Dependency] private readonly INullLinkPlayerManager _nullLinkPlayers = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PlayerRateLimitManager _rateLimit = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly ISharedAdminLogManager _alog = default!;

    private readonly Dictionary<Guid, MentorTicket> _tickets = [];
    private ISawmill _sawmill = default!;

    private readonly Dictionary<NetUserId, (TimeSpan Timestamp, bool Typing)> _typingUpdateTimestamps = [];

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("MHELP");

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        SubscribeNetworkEvent<MHelpTypingRequest>(OnClientTypingUpdated);
        SubscribeNetworkEvent<MHelpCloseTicket>(OnCloseTicket);
        SubscribeNetworkEvent<MhelpTptoTicket>(OnTptoTicket);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ =>
        {
            _tickets.Clear();
            _typingUpdateTimestamps.Clear();
        });

        _rateLimit.Register(
            RateLimitKey,
            new RateLimitRegistration(StarlightCCVars.MhelpRateLimitPeriod,
                StarlightCCVars.MhelpRateLimitCount,
                PlayerRateLimitedAction)
            );
    }
    private void PlayerRateLimitedAction(ICommonSession obj)
        => RaiseNetworkEvent(
            new MHelpTextMessage
            {
                Sender = obj.UserId,
                Ticket = null,
                Text = Loc.GetString("mentor-system-rate-limited"),
                PlaySound = false
            },
            obj.Channel);
    protected override void OnMentoringTextMessage(MHelpTextMessage message, EntitySessionEventArgs eventArgs)
    {
        MentorTicket? ticket = null;
        var senderSession = eventArgs.SenderSession;
        var adminData = _adminManager.GetAdminData(senderSession);

        var senderIsAdmin = adminData?.HasFlag(AdminFlags.Adminhelp) ?? false;
        var senderIsMentor = _playerRoles.IsMentor(senderSession);
        if (!senderIsAdmin && !senderIsMentor && _rateLimit.CountAction(senderSession, RateLimitKey) != RateLimitStatus.Allowed)
            return;
        
        if (message.Ticket is not Guid ticketId)
        {
            ticketId = Guid.NewGuid();

            ticket = new MentorTicket
            {
                Id = ticketId,
                Title = $"{senderSession.Name}[{ticketId.ToString()[..4]}]",
                Creator = senderSession.UserId
            };

            _tickets.Add(ticketId, ticket);

            var @event = new MHelpTextMessage()
            {
                Sender = senderSession.UserId,
                Ticket = ticketId,
                Title = ticket.Title,
                Text = Loc.GetString("mentor-system-ticket-created"),
                PlaySound = false
            };
            RaiseNetworkEvent(@event, senderSession.Channel);
        }
        if (ticket is null && !_tickets.TryGetValue(ticketId, out ticket))
            return;
        if (ticket.Creator != senderSession.UserId
        && ((ticket.Mentor is null && !senderIsMentor) || (ticket.Mentor != senderSession.UserId)) 
        && !(senderIsAdmin || senderIsMentor))
            return;
        var escapedText = FormattedMessage.EscapeText(message.Text);

        var text = senderIsAdmin ? $"{(message.PlaySound ? "" : "(S) ")}[color=#9B59B6][bold]\\[admin\\][/bold] {senderSession.Name}[/color]: {escapedText}"
                : senderIsMentor ? $"{(message.PlaySound ? "" : "(S) ")}[color=#00ffff][bold]\\[mentor\\][/bold] {senderSession.Name}[/color]: {escapedText}"
                                 : $"{(message.PlaySound ? "" : "(S) ")}{senderSession.Name}: {escapedText}";

        var msg = new MHelpTextMessage
        {
            Sender = senderSession.UserId,
            Ticket = ticketId,
            Text = text,
            Title = ticket.Title,
            PlaySound = true
        };
        _sawmill.Info($"mhelp message: {text}");
        if (ticket.Mentor is null && (senderIsMentor || senderIsAdmin))
        {
            ticket.Mentor = senderSession.UserId;
            var @event = new MHelpTextMessage()
            {
                Sender = senderSession.UserId,
                Ticket = ticketId,
                Title = ticket.Title,
                Text = Loc.GetString("mentor-system-ticket-claimed", ("name", senderSession.Name)),
                PlaySound = false,
                TicketClosed = true
            };
            var nonparticipantsRecipients = _nullLinkPlayers.Mentors
                .Except([_playerManager.GetSessionById(ticket.Creator), _playerManager.GetSessionById(ticket.Mentor.Value)])
                .Except(_adminManager.ActiveAdmins);
            foreach (var channel in nonparticipantsRecipients)
                RaiseNetworkEvent(@event, channel);

            @event.TicketClosed = false;
            var participantsRecipients = _adminManager.ActiveAdmins
                .Concat([_playerManager.GetSessionById(ticket.Creator), _playerManager.GetSessionById(ticket.Mentor.Value)])
                .Distinct();
            foreach (var channel in participantsRecipients)
                RaiseNetworkEvent(@event, channel);
        }

        var recipients = _adminManager.ActiveAdmins
            .Concat([_playerManager.GetSessionById(ticket.Creator)])
            .Concat(ticket.Mentor is not null
                ? [_playerManager.GetSessionById(ticket.Mentor.Value)]
                : _nullLinkPlayers.Mentors)
            .Distinct();

        foreach (var channel in recipients)
            RaiseNetworkEvent(msg, channel);
    }
    private void OnCloseTicket(MHelpCloseTicket message, EntitySessionEventArgs eventArgs)
    {
        MentorTicket? ticket = null;
        var senderSession = eventArgs.SenderSession;
        var adminData = _adminManager.GetAdminData(senderSession);

        var senderIsAdmin = adminData?.HasFlag(AdminFlags.Adminhelp) ?? false;
        var senderIsMentor = _playerRoles.IsMentor(senderSession);
        if (!senderIsAdmin && !senderIsMentor && _rateLimit.CountAction(senderSession, RateLimitKey) != RateLimitStatus.Allowed)
            return;
        if (message.Ticket is not Guid ticketId)
            return;
        if (ticket is null && !_tickets.TryGetValue(ticketId, out ticket))
            return;
        if (ticket.IsClosed)
            return;
        if (ticket.Creator != senderSession.UserId
            && (ticket.Mentor != senderSession.UserId)
            && !(senderIsAdmin || senderIsMentor))
            return;
        ticket.IsClosed = true;
        var msg = new MHelpTextMessage()
        {
            Sender = senderSession.UserId,
            Ticket = ticketId,
            Text = Loc.GetString("mentor-system-ticket-closed"),
            TicketClosed = true,
            PlaySound = false
        };

        _sawmill.Info($"mhelp ticket {ticketId} closed");

        var recipients = _adminManager.ActiveAdmins
            .Concat([_playerManager.GetSessionById(ticket.Creator)])
            .Concat(ticket.Mentor is not null
                ? [_playerManager.GetSessionById(ticket.Mentor.Value)]
                : _nullLinkPlayers.Mentors)
            .Distinct();

        foreach (var channel in recipients)
            RaiseNetworkEvent(msg, channel);
    }

    private void OnTptoTicket(MhelpTptoTicket message, EntitySessionEventArgs eventArgs)
    {
        MentorTicket? ticket = null;
        var senderSession = eventArgs.SenderSession;
        var adminData = _adminManager.GetAdminData(senderSession);

        var senderIsAdmin = adminData?.HasFlag(AdminFlags.Adminhelp) ?? false;
        var senderIsMentor = _playerRoles.IsMentor(senderSession);
        if (!(senderIsAdmin || senderIsMentor)) //only admins/mentors can use mtpto
            return;
        if (message.Ticket is not Guid ticketId)
            return;
        if (ticket is null && !_tickets.TryGetValue(ticketId, out ticket))
            return;
        var mentorEnt = senderSession.AttachedEntity;
        if (!HasComp<GhostComponent>(mentorEnt))
            return;
        
        var playerSession = _playerManager.GetSessionById(ticket.Creator);

        if (playerSession.AttachedEntity.HasValue)
            _xform.SetCoordinates(mentorEnt.Value, Transform(playerSession.AttachedEntity.Value).Coordinates);

        _alog.Add(LogType.AdminCommands, LogImpact.Low, $"mentor {senderSession} tpto'd ticket {ticketId} aka {playerSession}");
    }

    private void OnClientTypingUpdated(MHelpTypingRequest msg, EntitySessionEventArgs args)
    {
        if (_typingUpdateTimestamps.TryGetValue(args.SenderSession.UserId, out var tuple) &&
            tuple.Typing == msg.Typing &&
            tuple.Timestamp + TimeSpan.FromSeconds(1) > _timing.RealTime)
            return;

        _typingUpdateTimestamps[args.SenderSession.UserId] = (_timing.RealTime, msg.Typing);

        MentorTicket? ticket = null;
        var senderSession = args.SenderSession;

        if (ticket is null && !_tickets.TryGetValue(msg.Ticket, out ticket))
            return;

        var recipients = _adminManager.ActiveAdmins
            .Concat([_playerManager.GetSessionById(ticket.Creator)])
            .Concat(ticket.Mentor is not null
                ? [_playerManager.GetSessionById(ticket.Mentor.Value)]
                : _nullLinkPlayers.Mentors)
            .Except([senderSession])
            .Distinct();

        var update = new MHelpTypingUpdated
        {
            Typing = msg.Typing,
            Ticket = msg.Ticket,
            PlayerName = senderSession.Name
        };

        foreach (var channel in recipients)
            RaiseNetworkEvent(update, channel);
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        var message = e.NewStatus switch
        {
            SessionStatus.Connected => Loc.GetString("mentor-system-player-reconnecting"),
            SessionStatus.Disconnected => Loc.GetString("mentor-system-player-disconnecting"),
            _ => null
        };

        if (message == null) return;
        var tickets = _tickets.Values.Where(t => t.Creator == e.Session.UserId || t.Mentor == e.Session.UserId);
        foreach (var ticket in tickets)
        {
            var recipients = _adminManager.ActiveAdmins
                 .Concat(ticket.Mentor is not null
                     ? [_playerManager.GetSessionById(ticket.Mentor.Value)]
                     : _nullLinkPlayers.Mentors)
                 .Except([e.Session])
                 .Distinct();

            var msg = new MHelpTextMessage
            {
                Sender = e.Session.UserId,
                Ticket = ticket.Id,
                Text = message,
                PlaySound = false
            };

            foreach (var channel in recipients)
                RaiseNetworkEvent(msg, channel);
        }
    }
}

public sealed class MentorTicket
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public NetUserId Creator { get; set; }
    public NetUserId? Mentor { get; set; }
    public bool IsClosed { get; set; }
}
