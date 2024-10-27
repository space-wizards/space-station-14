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

    [DataField("color")]
    public Color? Color;

    [DataField("fontId")]
    public string? FontId;

    [DataField("fontSize")]
    public int? FontSize;

    /// <summary>
    /// 	If true, will mark the language as a SignLanguage and will be handled as such.
    /// </summary>
    [DataField("signLanguage")]
    public bool? SignLanguage;
}
