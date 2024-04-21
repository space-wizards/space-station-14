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

    protected override void Say(Entity<HailerComponent> ent, List<HailerLine> lines)
    {
        HailerLine line;
        do {
            line = _random.Pick(lines);
        } while (lines.Count > 1 && line.Message == ent.Comp.LastPlayed);

        _audio.PlayPvs(line.Sound, ent);
        _chat.TrySendInGameICMessage(ent, line.Message, InGameICChatType.Speak, ChatTransmitRange.GhostRangeLimit, checkRadioPrefix: false);
    }
}
