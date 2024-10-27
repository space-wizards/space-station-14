using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared._EinsteinEngine.Language.Components.Translators;

public abstract partial class BaseTranslatorComponent : Component
{
    /// <summary>
    ///   The list of additional languages this translator allows the wielder to speak.
    /// </summary>
    [DataField("spoken", customTypeSerializer: typeof(PrototypeIdListSerializer<LanguagePrototype>))]
    public List<string> SpokenLanguages = new();

    /// <summary>
    ///   The list of additional languages this translator allows the wielder to understand.
    /// </summary>
    [DataField("understood", customTypeSerializer: typeof(PrototypeIdListSerializer<LanguagePrototype>))]
    public List<string> UnderstoodLanguages = new();

    /// <summary>
    ///   The languages the wielding MUST know in order for this translator to have effect.
    ///   The field [RequiresAllLanguages] indicates whether all of them are required, or just one.
    /// </summary>
    [DataField("requires", customTypeSerializer: typeof(PrototypeIdListSerializer<LanguagePrototype>))]
    public List<string> RequiredLanguages = new();

    /// <summary>
    ///   If true, the wielder must understand all languages in [RequiredLanguages] to speak [SpokenLanguages],
    ///   and understand all languages in [RequiredLanguages] to understand [UnderstoodLanguages].
    ///
    ///   Otherwise, at least one language must be known (or the list must be empty).
    /// </summary>
    [DataField("requiresAll")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool RequiresAllLanguages = false;

    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;
}
