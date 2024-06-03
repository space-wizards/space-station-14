using Content.Server.Chat.Systems;
using Content.Shared.GreyStation.Hailer;
using Content.Shared.IdentityManagement;

namespace Content.Server.GreyStation.Hailer;

public sealed class HailerSystem : SharedHailerSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    protected override void Say(Entity<HailerComponent> ent, string message, EntityUid user)
    {
        var name = Loc.GetString("hailer-name", ("user", Identity.Name(user, EntityManager)));
        _chat.TrySendInGameICMessage(ent, message, InGameICChatType.Speak, ChatTransmitRange.GhostRangeLimit,
            checkRadioPrefix: false, nameOverride: name);
    }
}
