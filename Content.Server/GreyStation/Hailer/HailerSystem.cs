using Content.Server.Chat.Systems;
using Content.Shared.Dataset;
using Content.Shared.GreyStation.Hailer;
using Robust.Shared.Random;

namespace Content.Server.GreyStation.Hailer;

public sealed class HailerSystem : SharedHailerSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Say(EntityUid uid, DatasetPrototype dataset)
    {
        var message = _random.Pick(dataset.Values);
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, ChatTransmitRange.GhostRangeLimit, checkRadioPrefix: false);
    }
}
