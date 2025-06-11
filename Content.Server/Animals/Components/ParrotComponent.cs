namespace Content.Server.Animals.Components;

/// <summary>
/// Component for parroting behavior
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class ParrotComponent : Component
{
    /// <summary>
    /// List of speech entries this entity has learned
    /// </summary>
    [DataField]
    public List<string> SpeechMemory = new();

    /// <summary>
    /// Whether or not this parrot is listening for new entries
    /// </summary>
    [DataField]
    public bool Listening = false;

    /// <summary>
    /// Range of hearing for the entity
    /// </summary>
    [DataField]
    public int ListenRange = 10;

    /// <summary>
    /// The % chance an entity with this component learns a phrase when learning is off cooldown
    /// </summary>
    [DataField]
    public float LearnChance = 0.5f;

    /// <summary>
    /// Time in seconds before another attempt can be made at learning a phrase
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

    /// <summary>
    /// Minimum time to wait after speaking to speak again
    /// </summary>
    [DataField]
    public float MinSpeakInterval = 4.0f * 60f;

    /// <summary>
    /// Maximum time to wait after speaking to speak again
    /// </summary>
    [DataField]
    public float MaxSpeakInterval = 8.0f * 60f;

    /// <summary>
    /// Next time at which the parrot speaks
    /// </summary>
    [DataField]
    public TimeSpan NextSpeakInterval = TimeSpan.FromSeconds(0.0f);

    /// <summary>
    /// Odds of the parrot attempting to speak on the radio.
    /// </summary>
    [DataField]
    public float RadioAttemptChance = 0.3f;
}
