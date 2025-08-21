using Content.Shared.Chat;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Language;

[Prototype("language")]
public sealed partial class LanguagePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Icon of the language visible in chat/bubbles.
    /// </summary>
    [DataField("icon")]
    public ProtoId<JobIconPrototype> Icon = "LanguageIconUnknown";

    /// <summary>
    /// Show the Icon if understood.
    /// </summary>
    [DataField("iconUnderstood")]
    public bool IconVisibleIfUnderstood = true;

    /// <summary>
    /// Show the Icon if not understood.
    /// </summary>
    [DataField("iconNotUnderstood")]
    public bool IconVisibleIfNotUnderstood = true;

    /// <summary>
    ///     Obfuscation method used by this language. By default, uses <see cref="ObfuscationMethod.Default"/>.
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
    /// <summary>
    ///     Color which text in this language will be blended with.
    ///     Alpha blending is used, which means the alpha component of the color controls the intensity of this color.
    /// </summary>
    [DataField]
    public Color? Color = null;

    [DataField]
    public string? FontId;

    /// <summary>
    /// Only show the font when we Obfuscate the message (if not understood)
    /// </summary>
    [DataField]
    public bool? ObfuscationFont = false;

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
    ///     If true, requires the entity to have usable hands and be able to interact (not be cuffed, etc).
    /// </summary>
    [DataField]
    public bool RequireHands = false;

    /// <summary>
    ///     If not null, all messages in this language will be forced to be spoken in this chat type.
    /// </summary>
    [DataField]
    public InGameICChatType? ChatTypeOverride;

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
    public Dictionary<InGameICChatType, LocId> MessageWrapOverrides = new();
}