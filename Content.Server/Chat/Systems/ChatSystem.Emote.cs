using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.Systems;

public partial class ChatSystem
{
    private readonly Dictionary<string, EmotePrototype> _wordEmoteDict = new();

    private void InitializeEmotes()
    {
        _prototypeManager.PrototypesReloaded += OnPrototypeReloadEmotes;
        CacheEmotes();
    }

    private void ShutdownEmotes()
    {
        _prototypeManager.PrototypesReloaded -= OnPrototypeReloadEmotes;
    }

    private void OnPrototypeReloadEmotes(PrototypesReloadedEventArgs obj)
    {
        CacheEmotes();
    }

    private void CacheEmotes()
    {
        _wordEmoteDict.Clear();
        var emotes = _prototypeManager.EnumeratePrototypes<EmotePrototype>();
        foreach (var emote in emotes)
        {
            foreach (var word in emote.Words)
            {
                var lowerWord = word.ToLower();
                if (_wordEmoteDict.ContainsKey(lowerWord))
                {
                    var existingId = _wordEmoteDict[lowerWord].ID;
                    var errMsg = $"Duplicate of emote word {lowerWord} in emotes {emote.ID} and {existingId}";
                    Sawmill.Error(errMsg);
                    continue;
                }

                _wordEmoteDict.Add(lowerWord, emote);
            }
        }
    }

    public void TryEmoteWithChat(EntityUid uid, string emoteId, bool hideChat = true,
        bool hideGlobalGhostChat = false, string? nameOverride = null)
    {
        if (!_prototypeManager.TryIndex<EmotePrototype>(emoteId, out var proto))
            return;
        TryEmoteWithChat(uid, proto, hideChat, hideGlobalGhostChat, nameOverride);
    }

    public void TryEmoteWithChat(EntityUid uid, EmotePrototype proto, bool hideChat = true,
        bool hideGlobalGhostChat = false, string? nameOverride = null)
    {
        SendEntityEmote(uid, "temp", hideChat, hideGlobalGhostChat, nameOverride, false);
        TryEmoteWithoutChat(uid, proto);
    }

    public void TryEmoteWithoutChat(EntityUid uid, string emoteId)
    {
        if (!_prototypeManager.TryIndex<EmotePrototype>(emoteId, out var proto))
            return;
        TryEmoteWithoutChat(uid, proto);
    }

    public void TryEmoteWithoutChat(EntityUid uid, EmotePrototype proto)
    {
        if (!_actionBlocker.CanEmote(uid))
            return;

        InvokeEmoteEvent(uid, proto);
    }

    private void TryEmoteChatInput(EntityUid uid, string textInput)
    {
        var actionLower = textInput.ToLower();
        if (!_wordEmoteDict.TryGetValue(actionLower, out var emote))
            return;

        InvokeEmoteEvent(uid, emote);
    }

    private void InvokeEmoteEvent(EntityUid uid, EmotePrototype proto)
    {
        var ev = new EmoteEvent(proto);
        RaiseLocalEvent(uid, ref ev);
    }

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

[ByRefEvent]
public struct EmoteEvent
{
    public bool Handled;
    public readonly EmotePrototype Emote;

    public EmoteEvent(EmotePrototype emote)
    {
        Emote = emote;
        Handled = false;
    }
}
