using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chat.V2.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

public partial class SharedChatSystem
{
    private FrozenDictionary<string, EmotePrototype> _wordEmoteDict = FrozenDictionary<string, EmotePrototype>.Empty;

    private void InitializeEmote()
    {
        _prototype.PrototypesReloaded += OnPrototypesReloaded;
        CacheEmotes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EmotePrototype>())
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
                if (dict.TryGetValue(lowerWord, out _))
                {
                    continue;
                }

                dict.Add(lowerWord, emote);
            }
        }

        _wordEmoteDict = dict.ToFrozenDictionary();
    }

    protected EmotePrototype? GetEmote(string name)
    {
        return !_wordEmoteDict.TryGetValue(name.ToLower(), out var emote) ? null : emote;
    }

    public bool SendEmoteMessage(EntityUid emoter, string message, [NotNullWhen(false)] out string? reason)
    {
        if (!TryComp<EmoteableComponent>(emoter, out _))
        {
            reason = Loc.GetString("chat-system-emote-failed");

            return false;
        }

        if (message.Length > MaxChatMessageLength)
        {
            reason = Loc.GetString("chat-system-max-message-length");

            return false;
        }

        RaiseNetworkEvent(new AttemptEmoteEvent(GetNetEntity(emoter), message));

        reason = null;

        return true;
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

/// <summary>
/// Raised when a mob tries to emote.
/// </summary>
[Serializable, NetSerializable]
public sealed class AttemptEmoteEvent : EntityEventArgs
{
    public NetEntity Emoter;
    public readonly string Message;

    public AttemptEmoteEvent(NetEntity emoter, string message)
    {
        Emoter = emoter;
        Message = message;
    }
}

/// <summary>
/// Raised when a mob emotes.
/// </summary>
[Serializable, NetSerializable]
public sealed class EmoteNetworkEvent : EntityEventArgs
{
    public NetEntity Emoter;
    public string AsName;
    public readonly string Message;

    public EmoteNetworkEvent(NetEntity emoter, string asName,string message)
    {
        Emoter = emoter;
        AsName = asName;
        Message = message;
    }
}

/// <summary>
/// Raised when a mob has failed to emote.
/// </summary>
[Serializable, NetSerializable]
public sealed class EmoteFailedEvent : EntityEventArgs
{
    public NetEntity Emoter;
    public readonly string Reason;

    public EmoteFailedEvent(NetEntity emoter, string reason)
    {
        Emoter = emoter;
        Reason = reason;
    }
}
