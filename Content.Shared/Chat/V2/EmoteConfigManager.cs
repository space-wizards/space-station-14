using System.Collections.Frozen;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.V2;

/// <summary>
/// A manager that allows for access to the <see cref="EmotePrototype"/> datastore.
/// </summary>
public interface IEmoteConfigManager
{
    public void Initialize();

    public EmotePrototype? GetEmote(string name);
}

/// <summary>
/// Contains and controls emote configuration based on the <see cref="EmotePrototype"/> datastore.
/// When prototypes are reloaded this config cache is refreshed.
/// </summary>
public sealed class EmoteConfigManager : IEmoteConfigManager
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private FrozenDictionary<string, EmotePrototype> _wordEmoteDict = FrozenDictionary<string, EmotePrototype>.Empty;

    public void Initialize()
    {
        _prototype.PrototypesReloaded += OnPrototypesReloaded;
        CacheEmotes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EmotePrototype>()) {}
            CacheEmotes();
    }

    private void CacheEmotes()
    {
        var dict = new Dictionary<string, EmotePrototype>();
        var emotes = _prototype.EnumeratePrototypes<EmotePrototype>();
        foreach (var emote in emotes)
        {
            foreach (var word in emote.ChatTriggers)
            {
                var lowerWord = word.ToLower();
                if (dict.TryGetValue(lowerWord, out var value))
                {
                    continue;
                }

                dict.Add(lowerWord, emote);
            }
        }

        _wordEmoteDict = dict.ToFrozenDictionary();
    }

    public EmotePrototype? GetEmote(string name)
    {
        return !_wordEmoteDict.TryGetValue(name.ToLower(), out var emote) ? null : emote;
    }
}
