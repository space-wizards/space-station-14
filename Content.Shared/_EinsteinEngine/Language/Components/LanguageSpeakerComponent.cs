using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared._EinsteinEngine.Language.Components;

// TODO: either move all language speaker-related components to server side, or make everything else shared.
// The current approach leads to confusion, as the server never informs the client of updates in these components.

/// <summary>
///     Stores the current state of the languages the entity can speak and understand.
/// </summary>
/// <remarks>
///     All fields of this component are populated during a DetermineEntityLanguagesEvent.
///     They are not to be modified externally.
/// </remarks>
[RegisterComponent]
public sealed partial class LanguageSpeakerComponent : Component
{
    /// <summary>
    ///     The current language the entity uses when speaking.
    ///     Other listeners will hear the entity speak in this language.
    /// </summary>
    [DataField]
    public string CurrentLanguage = ""; // The language system will override it on init

    /// <summary>
    ///     List of languages this entity can speak at the current moment.
    /// </summary>
    public List<string> SpokenLanguages = [];

    /// <summary>
    ///     List of languages this entity can understand at the current moment.
    /// </summary>
    public List<string> UnderstoodLanguages = [];
}
