namespace Content.Shared._EinsteinEngine.Language.Components.Translators;

/// <summary>
///   A translator that must be held in a hand or a pocket of an entity in order ot have effect.
/// </summary>
[RegisterComponent]
public sealed partial class HandheldTranslatorComponent : Translators.BaseTranslatorComponent
{
    /// <summary>
    ///   Whether or not interacting with this translator
    ///   toggles it on or off.
    /// </summary>
    [DataField]
    public bool ToggleOnInteract = true;

    /// <summary>
    ///     If true, when this translator is turned on, the entities' current spoken language will be set
    ///     to the first new language added by this translator.
    /// </summary>
    /// <remarks>
    ///      This should generally be used for translators that translate speech between two languages.
    /// </remarks>
    [DataField]
    public bool SetLanguageOnInteract = true;
}
