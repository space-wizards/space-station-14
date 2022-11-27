using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.Prototypes;

/// <summary>
///     IC emotes (scream, smile, clapping, etc).
///     Entities can activate emotes by chat input or code.
/// </summary>
[Prototype("emote")]
public sealed class EmotePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    ///     Different emote categories may be handled by different systems.
    ///     Also may be used for filtering.
    /// </summary>
    [DataField("category")]
    public EmoteCategory Category = EmoteCategory.General;

    /// <summary>
    ///     Collection of words that will be send to chat if emote will be activated.
    ///     One of this words will be picked randomly.
    /// </summary>
    [DataField("chatMessages")]
    public List<string> ChatMessages = new();

    /// <summary>
    ///     Trigger words for emote. Case independent.
    ///     When typed into players chat they will activate emote event.
    ///     All words should be unique across all emote prototypes.
    /// </summary>
    [DataField("chatTriggers")]
    public HashSet<string> ChatTriggers = new();
}

/// <summary>
///     IC emote category. Usually source of emote,
///     like hands, vocal, facial, etc.
/// </summary>
[Flags]
[Serializable, NetSerializable]
public enum EmoteCategory : byte
{
    Invalid = 0,
    Vocal = 1 << 0,
    Hands = 1 << 1,
    General = byte.MaxValue
}
