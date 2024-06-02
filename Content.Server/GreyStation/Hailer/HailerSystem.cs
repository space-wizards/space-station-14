using Content.Server.Chat.Systems;
using Content.Shared.GreyStation.Hailer;

namespace Content.Server.GreyStation.Hailer;

public sealed class HailerSystem : SharedHailerSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    protected override void Say(Entity<HailerComponent> ent, string message)
    {
        _chat.TrySendInGameICMessage(ent, message, InGameICChatType.Speak, ChatTransmitRange.GhostRangeLimit, checkRadioPrefix: false);
    }
}
