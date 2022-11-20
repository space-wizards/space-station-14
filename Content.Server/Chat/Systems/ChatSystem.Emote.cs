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

    public void CacheEmotes()
    {
        _wordEmoteDict.Clear();
        var emotes = _prototypeManager.EnumeratePrototypes<EmotePrototype>();
        foreach (var emote in emotes)
        {
            foreach (var word in emote.Words)
            {
                if (_wordEmoteDict.ContainsKey(word))
                {
                    var existingId = _wordEmoteDict[word].ID;
                    Sawmill.Error($"Duplicate of emote word {word} in emotes {emote.ID} and {existingId}");
                    continue;
                }

                _wordEmoteDict.Add(word, emote);
            }
        }
    }

    public bool TryEmote(EntityUid uid, string action)
    {
        var actionLower = action.ToLower();
        if (!_wordEmoteDict.TryGetValue(actionLower, out var emote))
            return false;

        var ev = new EmoteEvent(emote);
        RaiseLocalEvent(uid, ref ev);

        return ev.Handled;
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
