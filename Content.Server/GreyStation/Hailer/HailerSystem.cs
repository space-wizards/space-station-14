using Content.Server.Chat.Systems;
using Content.Shared.GreyStation.Hailer;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.GreyStation.Hailer;

public sealed class HailerSystem : SharedHailerSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    protected override void Say(EntityUid uid, List<HailerLine> lines)
    {
        var line = _random.Pick(lines);
        _audio.PlayPvs(line.Sound, uid);
        _chat.TrySendInGameICMessage(uid, line.Message, InGameICChatType.Speak, ChatTransmitRange.GhostRangeLimit, checkRadioPrefix: false);
    }
}
