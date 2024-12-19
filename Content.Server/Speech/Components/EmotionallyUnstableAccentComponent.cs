namespace Content.Server.Speech.Components;

using Content.Shared.Chat.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

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
    public float TriggerEmotionChance = 0.5f;
    
    /// <summary>
    /// The probability of a message being supplemented with punctuation marks.
    /// </summary>
    [DataField]
    public float ChangeMessageChance = 1.0f;

    /// <summary> 
    /// A set of emotes that will be randomly picked from. 
    /// <see cref="EmotePrototype"/> 
    /// </summary> 
    [DataField("emotes", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<EmotePrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public HashSet<string> Emotes = new();
}
