namespace Content.Server.Animals.Components;

/// <summary>
/// Makes an entity able to memorize chat/radio messages
/// </summary>
[RegisterComponent]
public sealed partial class ParrotMemoryComponent : Component
{
    /// <summary>
    /// List of speech entries this entity has learned
    /// </summary>
    [DataField]
    public List<string> SpeechMemory = new();

    /// <summary>
    /// The % chance an entity with this component learns a phrase when learning is off cooldown
    /// </summary>
    [DataField]
    public float LearnChance = 0.4f;

    /// <summary>
    /// Time after which another attempt can be made at learning a phrase
    /// </summary>
    [DataField]
    public TimeSpan LearnCooldown = TimeSpan.FromSeconds(60f);

    /// <summary>
    /// Next time at which the parrot can attempt to learn something
    /// </summary>
    [DataField]
    public TimeSpan NextLearnInterval = TimeSpan.FromSeconds(0.0f);

    /// <summary>
    /// The number of speech entries that are remembered
    /// </summary>
    [DataField]
    public int MaxSpeechMemory = 50;

    /// <summary>
    /// Minimum length of a speech entry
    /// </summary>
    [DataField]
    public int MinEntryLength = 4;

    /// <summary>
    /// Maximum length of a speech entry
    /// </summary>
    [DataField]
    public int MaxEntryLength = 50;
}
