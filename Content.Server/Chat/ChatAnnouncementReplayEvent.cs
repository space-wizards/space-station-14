using Content.Server.GameTicking.Replays;

namespace Content.Server.Chat;

[Serializable, DataDefinition]
public sealed partial class ChatAnnouncementReplayEvent : ReplayEvent
{
    [DataField]
    public string Message = string.Empty;

    [DataField]
    public string Sender = string.Empty;
}
