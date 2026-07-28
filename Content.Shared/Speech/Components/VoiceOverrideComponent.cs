using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.Components;

/// <summary>
///     Will change the voice of the entity that has the component (e.g radio and speech).
/// </summary>
/// <remarks>
///     Before using this component, please take a look at the the TransformSpeakerNameEvent (and the inventory relay version).
///     Depending on what you're doing, it could be a better choice!
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class VoiceOverrideComponent : Component
{
    /// <summary>
    ///     The name that will be used instead of an entities default one.
    ///     Uses the localized version of the string and if null wont do anything.
    /// </summary>
    [DataField]
    public string? NameOverride;

    /// <summary>
    ///     The verb that will be used insteand of an entities default one.
    ///     If null, the defaut will be used.
    /// </summary>
    [DataField]
    public ProtoId<SpeechVerbPrototype>? SpeechVerbOverride;

    /// <summary>
    ///     If true, the override values (if they are not null) will be applied.
    /// </summary>
    [DataField]
    public bool Enabled = true;
}
