using System.Text;
using Content.Shared._Starlight.Language.Systems;

namespace Content.Shared._Starlight.Language;

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
///  Obfuscate the string letters/numbers to random, keeps special characters and spaces.
/// </summary>
public partial class RandomObfuscation : ObfuscationMethod
{
    internal override void Obfuscate(StringBuilder builder, string message, SharedLanguageSystem context)
    {
        const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        message = message.ToUpper();

        for (int i = 0; i < Chars.Length; i++)
        {
            message = message.Replace(Chars[i], Chars[context.PseudoRandomNumber(message.GetHashCode() + i, 0, Chars.Length - 1)]);
        }

        builder.Append(message);
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
        const char eof = (char) 0; // Special character to mark the end of the message in the code below.

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
