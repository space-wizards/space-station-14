using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.Prototypes;

/// <summary>
///     IC emotes (scream, smile, clapping, etc).
///     Entities can activate emotes by chat input or code.
/// </summary>
[Prototype("emote")]
public sealed partial class EmotePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Different emote categories may be handled by different systems.
    ///     Also may be used for filtering.
    /// </summary>
    [DataField]
    public EmoteCategory Category = EmoteCategory.General;

    /// <summary>
    /// An icon used to visually represent the emote in radial UI.
    /// </summary>
    [DataField]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;

    [DataField]
    public bool Available = true;

    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    ///     Collection of words that will be sent to chat if emote activates.
    ///     Will be picked randomly from list.
    /// </summary>
    [DataField]
    public List<string> ChatMessages = new();

    /// <summary>
    ///     Trigger words for emote. Case independent.
    ///     When typed into players chat they will activate emote event.
    ///     All words should be unique across all emote prototypes.
    /// </summary>
    [DataField]
    public HashSet<string> ChatTriggers = new();
}

/// <summary>
///     IC emote category. Usually physical source of emote,
///     like hands, voice, face, etc.
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
