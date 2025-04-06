using Content.Server._NF.Speech.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Server._NF.Speech.Components;

[RegisterComponent]
[Access(typeof(ParrotSpeechSystem))]
public sealed partial class ParrotSpeechComponent : Component
{
    /// <summary>
    /// The maximum number of words the parrot can learn per phrase.
    /// Phrases are 1 to MaxPhraseLength words in length.
    /// </summary>
    [DataField]
    public int MaximumPhraseLength = 7;

    [DataField]
    public int MaximumPhraseCount = 10;

    [DataField]
    public int MinimumWait = 120; // 120 / 60 = 2 minutes

    [DataField]
    public int MaximumWait = 300; // 300 / 60 = 5 minutes

    /// <summary>
    /// The probability that a parrot will learn from something an overheard phrase.
    /// </summary>
    [DataField]
    public float LearnChance = 0.2f;

    [DataField]
    public EntityWhitelist Blacklist { get; private set; } = new();

    [DataField]
    public TimeSpan? NextUtterance;

    [DataField(readOnly: true)]
    public List<string> LearnedPhrases = new();

    [DataField] // imp. be very careful with this one. if it ends up being a problem even once, it should be set to true on that entity.
    public bool HideMessagesInChat = true;

    [DataField] // imp
    public bool RequiresMind = true;

    /// <summary>
    /// Preserves capitalization in memorized messages and appends a "..." to the start and end of sentence fragments.
    /// For signifying the context recalled by smarter entities who aren't just repeating sounds (knowingly repeating words).
    /// </summary>
    [DataField] // imp
    public bool PreserveContext = false;

    /// <summary>
    /// Percentage chance the echo will be a whisper instead of normal speech. Nervous stimming getting mistaken as syndicate clues. Y'know.
    /// 1.0 = Always whispering. 0.0 = Doesn't whisper.
    /// </summary>
    [DataField] // imp
    public float WhisperChance = 0.0f;

    [DataField] // imp
    public bool FakeTypingIndicator = true;

    /// <summary>
    ///  the next time the fake typing indicator will end and a message will be sent
    /// </summary>
    public TimeSpan? NextFakeTypingSend = null; // imp

    public string? NextMessage = null; // imp
}
