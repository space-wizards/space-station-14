using System.Text;
using Content.Shared._EinsteinEngine.Language.Systems;

namespace Content.Shared._EinsteinEngine.Language;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ObfuscationMethod
{
    /// <summary>
    ///     The fallback obfuscation method, replaces the message with the string "&lt;?&gt;".
    /// </summary>
    public static readonly ObfuscationMethod Default = new ReplacementObfuscation
    {
        Replacement = new List<string> { "<?>" }
    };

    /// <summary>
    ///     Obfuscates the provided message and writes the result into the provided StringBuilder.
    ///     Implementations should use the context's pseudo-random number generator and provide stable obfuscations.
    /// </summary>
    internal abstract void Obfuscate(StringBuilder builder, string message, SharedLanguageSystem context);

    /// <summary>
    ///     Obfuscates the provided message. This method should only be used for debugging purposes.
    ///     For all other purposes, use <see cref="SharedLanguageSystem.ObfuscateSpeech"/> instead.
    /// </summary>
    public string Obfuscate(string message)
    {
        var builder = new StringBuilder();
        Obfuscate(builder, message, IoCManager.Resolve<EntitySystemManager>().GetEntitySystem<SharedLanguageSystem>());
        return builder.ToString();
    }
}

/// <summary>
///     The most primitive method of obfuscation - replaces the entire message with one random replacement phrase.
///     Similar to ReplacementAccent. Base for all replacement-based obfuscation methods.
/// </summary>
public partial class ReplacementObfuscation : ObfuscationMethod
{
    /// <summary>
    ///     A list of replacement phrases used in the obfuscation process.
    /// </summary>
    [DataField(required: true)]
    public List<string> Replacement = [];

    internal override void Obfuscate(StringBuilder builder, string message, SharedLanguageSystem context)
    {
        var idx = context.PseudoRandomNumber(message.GetHashCode(), 0, Replacement.Count - 1);
        builder.Append(Replacement[idx]);
    }
}

/// <summary>
///     Obfuscates the provided message by replacing each word with a random number of syllables in the range (min, max),
///     preserving the original punctuation to a resonable extent.
/// </summary>
/// <remarks>
///     The words are obfuscated in a stable manner, such that every particular word will be obfuscated the same way throughout one round.
///     This means that particular words can be memorized within a round, but not across rounds.
/// </remarks>
public sealed partial class SyllableObfuscation : ReplacementObfuscation
{
    [DataField]
    public int MinSyllables = 1;

    [DataField]
    public int MaxSyllables = 4;

    internal override void Obfuscate(StringBuilder builder, string message, SharedLanguageSystem context)
    {
        const char eof = (char) 0; // Special character to mark the end of the message in the code below

        var wordBeginIndex = 0;
        var hashCode = 0;

        for (var i = 0; i <= message.Length; i++)
        {
            var ch = i < message.Length ? char.ToLower(message[i]) : eof;
            var isWordEnd = char.IsWhiteSpace(ch) || IsPunctuation(ch) || ch == eof;

            // If this is a normal char, add it to the hash sum
            if (!isWordEnd)
                hashCode = hashCode * 31 + ch;

            // If a word ends before this character, construct a new word and append it to the new message.
            if (isWordEnd)
            {
                var wordLength = i - wordBeginIndex;
                if (wordLength > 0)
                {
                    var newWordLength = context.PseudoRandomNumber(hashCode, MinSyllables, MaxSyllables);

                    for (var j = 0; j < newWordLength; j++)
                    {
                        var index = context.PseudoRandomNumber(hashCode + j, 0, Replacement.Count - 1);
                        builder.Append(Replacement[index]);
                    }
                }

                hashCode = 0;
                wordBeginIndex = i + 1;
            }

            // If this message concludes a word (i.e. is a whitespace or a punctuation mark), append it to the message
            if (isWordEnd && ch != eof)
                builder.Append(ch);
        }
    }

    private static bool IsPunctuation(char ch)
    {
        return ch is '.' or '!' or '?' or ',' or ':';
    }
}

/// <summary>
///     Obfuscates each sentence in the message by concatenating a number of obfuscation phrases.
///     The number of phrases in the obfuscated message is proportional to the length of the original message.
/// </summary>
public sealed partial class PhraseObfuscation : ReplacementObfuscation
{
    [DataField]
    public int MinPhrases = 1;

    [DataField]
    public int MaxPhrases = 4;

    /// <summary>
    ///     A string used to separate individual phrases within one sentence. Default is a space.
    /// </summary>
    [DataField]
    public string Separator = " ";

    /// <summary>
    ///     A power to which the number of characters in the original message is raised to determine the number of phrases in the result.
    ///     Default is 1/3, i.e. the cubic root of the original number.
    /// </summary>
    /// <remarks>
    ///     Using the default proportion, you will need at least 27 characters for 2 phrases, at least 64 for 3, at least 125 for 4, etc.
    ///     Increasing the proportion to 1/4 will result in the numbers changing to 81, 256, 625, etc.
    /// </remarks>
    [DataField]
    public float Proportion = 1f / 3;

    internal override void Obfuscate(StringBuilder builder, string message, SharedLanguageSystem context)
    {
        var sentenceBeginIndex = 0;
        var hashCode = 0;

        for (var i = 0; i < message.Length; i++)
        {
            var ch = char.ToLower(message[i]);
            if (!IsPunctuation(ch) && i != message.Length - 1)
            {
                hashCode = hashCode * 31 + ch;
                continue;
            }

            var length = i - sentenceBeginIndex;
            if (length > 0)
            {
                var newLength = (int) Math.Clamp(Math.Pow(length, Proportion) - 1, MinPhrases, MaxPhrases);

                for (var j = 0; j < newLength; j++)
                {
                    var phraseIdx = context.PseudoRandomNumber(hashCode + j, 0, Replacement.Count - 1);
                    var phrase = Replacement[phraseIdx];
                    builder.Append(phrase);
                    builder.Append(Separator);
                }
            }
            sentenceBeginIndex = i + 1;

            if (IsPunctuation(ch))
                builder.Append(ch).Append(' '); // TODO: this will turn '...' into '. . . '
        }
    }

    private static bool IsPunctuation(char ch)
    {
        return ch is '.' or '!' or '?'; // Doesn't include mid-sentence punctuation like the comma
    }
}
