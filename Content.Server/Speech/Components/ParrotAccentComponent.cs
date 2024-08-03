namespace Content.Server.Speech.Components;

/// <summary>
/// Makes this entity speak like a parrot in all chat messages it sends.
/// </summary>
[RegisterComponent]
public sealed partial class ParrotAccentComponent : Component
{
    /// <summary>
    /// Chance that a message will have a squawk sound added before the first character.
    /// If it fails, the message with have a squawk as a postfix instead.
    /// If the longest word is repeated, no pre- or postfix will be added.
    /// </summary>
    [DataField]
    public float SquawkPrefixChance = 0.5f;

    /// <summary>
    /// Chance that the longest word in the message will be repeated as an
    /// exclamation at the end of the final message.
    /// </summary>
    [DataField]
    public float LongestWordRepeatChance = 0.5f;

    /// <summary>
    /// The longest word must be at least this many characters long to be
    /// repeated. This prevents repeating short words, which can sound weird.
    /// ex: "How are you? AWWK! How!" - bad
    /// ex: "Look out, it's the captain! RAWWK! Captain!" - good
    /// </summary>
    [DataField]
    public float LongestWordMinLength = 5;

    /// <summary>
    /// Strings to use as squawking noises.
    /// </summary>
    public readonly string[] Squawks = [
        "accent-parrot-squawk-1",
        "accent-parrot-squawk-2",
        "accent-parrot-squawk-3",
        "accent-parrot-squawk-4",
        "accent-parrot-squawk-5",
        "accent-parrot-squawk-6",
        "accent-parrot-squawk-7",
        "accent-parrot-squawk-8"
    ];

}
