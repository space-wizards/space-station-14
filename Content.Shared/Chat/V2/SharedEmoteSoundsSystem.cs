using Content.Shared.Chat.Prototypes;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Chat.V2;

public sealed class SharedEmoteSoundsSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public bool TryPlayEmoteSound(EntityUid uid, EmoteSoundsPrototype? proto, EmotePrototype emote)
    {
        return TryPlayEmoteSound(uid, proto, emote.ID);
    }

    public bool TryPlayEmoteSound(EntityUid uid, EmoteSoundsPrototype? proto, string emoteId)
    {
        if (proto == null)
            return false;

        // try to get specific sound for this emote
        if (!proto.Sounds.TryGetValue(emoteId, out var sound))
        {
            // no specific sound - check fallback
            sound = proto.FallbackSound;
            if (sound == null)
                return false;
        }

        // if general params for all sounds set - use them
        var param = proto.GeneralParams ?? sound.Params;

        _audio.PlayPvs(sound, uid, param);

        return true;
    }
}
