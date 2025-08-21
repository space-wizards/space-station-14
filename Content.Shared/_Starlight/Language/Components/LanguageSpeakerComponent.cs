using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Shared._Starlight.Language.Components;

/// <summary>
///     Stores the current state of the languages the entity can speak and understand.
/// </summary>
/// <remarks>
///     All fields of this component are populated during a DetermineEntityLanguagesEvent.
///     They are not to be modified externally.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class LanguageSpeakerComponent : Component
{
    public override bool SendOnlyToOwner => true;

    /// <summary>
    ///     The current language the entity uses when speaking.
    ///     Other listeners will hear the entity speak in this language.
    /// </summary>
    [DataField]
    public string CurrentLanguage = ""; // The Language system will override it on mapinit

    /// <summary>
    ///     List of languages this entity can speak at the current moment.
    /// </summary>
    [DataField]
    public List<ProtoId<LanguagePrototype>> SpokenLanguages = [];

    /// <summary>
    ///     List of languages this entity can understand at the current moment.
    /// </summary>
    [DataField]
    public List<ProtoId<LanguagePrototype>> UnderstoodLanguages = [];

    [Serializable, NetSerializable]
    public sealed class State : ComponentState
    {
        public string CurrentLanguage = default!;
        public List<ProtoId<LanguagePrototype>> SpokenLanguages = default!;
        public List<ProtoId<LanguagePrototype>> UnderstoodLanguages = default!;
    }
}