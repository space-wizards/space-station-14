#nullable enable
using Content;
using Content.Shared.Starlight.MHelp;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client._Starlight.MHelp;

[UsedImplicitly]
public sealed class MentorSystem : SharedMentorSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public event EventHandler<MHelpTextMessage>? OnMentoringTextMessageReceived;
    private (TimeSpan Timestamp, bool Typing) _lastTypingUpdateSent;

    protected override void OnMentoringTextMessage(MHelpTextMessage message, EntitySessionEventArgs eventArgs)
    {
        OnMentoringTextMessageReceived?.Invoke(this, message);
    }

    public void Send(Guid? ticket, string text, bool playSound)
    {
        RaiseNetworkEvent(new MHelpTextMessage
        {
            Ticket = ticket,
            Text = text,
            PlaySound = playSound
        });
        SendInputTextUpdated(ticket, false);
    }

    public void SendInputTextUpdated(Guid? ticket, bool typing)
    {
        if (ticket is null || (_lastTypingUpdateSent.Typing == typing &&
            _lastTypingUpdateSent.Timestamp + TimeSpan.FromSeconds(1) > _timing.RealTime))
            return;

        _lastTypingUpdateSent = (_timing.RealTime, typing);
        RaiseNetworkEvent(new MHelpTypingRequest()
        {
            Ticket = ticket.Value,
            Typing = typing
        });
    }

    internal void SendCloseTicket(Guid ticket)
        => RaiseNetworkEvent(new MHelpCloseTicket
        {
            Ticket = ticket,
        });

    internal void SentTpto(Guid ticket)
        => RaiseNetworkEvent(new MhelpTptoTicket
        {
            Ticket = ticket,
        });
}
