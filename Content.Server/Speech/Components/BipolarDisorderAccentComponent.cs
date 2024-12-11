namespace Content.Server.Speech.Components;

/// <summary>
/// Bipolar disorder accent adds random punctuation ("!", "...", "?!") to the end of phrases, and randomly triggers the emotions selected in the component.
/// </summary>
[RegisterComponent]
public sealed partial class BipolarDisorderAccentComponent : Component
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
    /// A list of available emotions for playback.
    /// </summary>
    [DataField]
    public List<string> Emotions = new();

    /// <summary>
    /// The index of the next emotion from the "Emotions" list.
    /// </summary>
    public int NextEmotionIndex = 0;
}
