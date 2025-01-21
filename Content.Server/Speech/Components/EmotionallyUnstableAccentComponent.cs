using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Speech.Components;

/// <summary>
/// This component adds random punctuation ("!", "...", "?!") to the end of phrases, and randomly triggers the emotions selected in the component.
/// </summary>
[RegisterComponent]
public sealed partial class EmotionallyUnstableAccentComponent : Component
{
    /// <summary>
    /// The probability of triggering an emotion from the "Emotions" list after each phrase.
    /// </summary>
    [DataField]
    public float TriggerEmotionChance = 0.2f;

    /// <summary>
    /// This variable represents the chance that a sentence will become exclamatory rather than contemplative.
    /// It is by no means the same as "TriggerEmotionChance", as this variable specifically determines the chance of an exclamatory sentence,
    /// not the actual modification of the sentence itself.
    /// </summary>
    [DataField]
    public float ExclamatorySentenceChance = 0.5f;
    
    /// <summary>
    /// The probability of a message being supplemented with punctuation marks.
    /// </summary>
    [DataField]
    public float ChangeMessageChance = 0.3f;

    /// <summary> 
    /// A set of emotes that will be randomly picked from.
    /// </summary> 
    [DataField]
    public List<ProtoId<EmotePrototype>> Emotes = new();
}
