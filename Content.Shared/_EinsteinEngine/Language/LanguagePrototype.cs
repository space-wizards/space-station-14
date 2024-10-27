using Content.Shared.Chat;
using Robust.Shared.Prototypes;

namespace Content.Shared._EinsteinEngine.Language;

[Prototype("language")]
public sealed class LanguagePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set;  } = default!;

    /// <summary>
    ///     Obfuscation method used by this language. By default, uses <see cref="ObfuscationMethod.Default"/>
    /// </summary>
    [DataField("obfuscation")]
    public ObfuscationMethod Obfuscation = ObfuscationMethod.Default;

    /// <summary>
    ///     Speech overrides used for messages sent in this language.
    /// </summary>
    [DataField("speech")]
    public SpeechOverrideInfo SpeechOverride = new();

    #region utility
    /// <summary>
    ///     The in-world name of this language, localized.
    /// </summary>
    public string Name => Loc.GetString($"language-{ID}-name");

    /// <summary>
    ///     The in-world description of this language, localized.
    /// </summary>
    public string Description => Loc.GetString($"language-{ID}-description");
    #endregion utility
}

[DataDefinition]
public sealed partial class SpeechOverrideInfo
{
    [DataField]
    public Color Color = Color.White;

    [DataField]
    public string? FontId;

    [DataField]
    public int? FontSize;

    [DataField]
    public bool AllowRadio = true;

    /// <summary>
    ///     If false, the entity can use this language even when it's unable to speak (i.e. muffled or muted),
    ///     and accents are not applied to messages in this language.
    /// </summary>
    [DataField]
    public bool RequireSpeech = true;

    /// <summary>
    ///     If not null, all messages in this language will be forced to be spoken in this chat type.
    /// </summary>
    [DataField]
    public SharedChatSystem.InGameICChatType? ChatTypeOverride;

    /// <summary>
    ///     Speech verb overrides. If not provided, the default ones for the entity are used.
    /// </summary>
    [DataField]
    public List<LocId>? SpeechVerbOverrides;

    /// <summary>
    ///     Overrides for different kinds chat message wraps. If not provided, the default ones are used.
    /// </summary>
    /// <remarks>
    ///     Currently, only local chat and whispers support this. Radio and emotes are unaffected.
    ///     This is horrible.
    /// </remarks>
    [DataField]
    public Dictionary<SharedChatSystem.InGameICChatType, LocId> MessageWrapOverrides = new();
}
