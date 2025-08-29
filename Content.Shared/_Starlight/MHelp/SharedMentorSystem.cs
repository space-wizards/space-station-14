#nullable enable
using Content;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.MHelp;

public abstract class SharedMentorSystem : EntitySystem
{
    // System users
    public static NetUserId SystemUserId { get; } = new NetUserId(Guid.Empty);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<MHelpTextMessage>(OnMentoringTextMessage);
    }

    protected virtual void OnMentoringTextMessage(MHelpTextMessage message, EntitySessionEventArgs eventArgs)
    {
        // Specific side code in target.
    }

    protected void LogMentoring(MHelpTextMessage message)
    {
    }

    [Serializable, NetSerializable]
    public sealed class MHelpTextMessage() : EntityEventArgs
    {
        public NetUserId? Sender { get; init; }
        public Guid? Ticket { get; init; }
        public DateTime CreateAt { get; init; } = DateTime.UtcNow;
        public required string Text { get; init; }
        public bool PlaySound { get; init; }
        public bool TicketClosed { get; set; }
        public string Title { get; set; } = "";
    }
    [Serializable, NetSerializable]
    public sealed class MHelpCloseTicket() : EntityEventArgs
    {
        public Guid? Ticket { get; init; }
    }
    
    [Serializable, NetSerializable]
    public sealed class MhelpTptoTicket() : EntityEventArgs
    {
        public Guid? Ticket { get; init; }
    }
}
