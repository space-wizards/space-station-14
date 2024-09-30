using System.Collections.Frozen;
using System.Linq;
using System.Text;
using System.Text.Unicode;

namespace Content.Shared.Chat.V2.Moderation;

/// <summary>
/// A basic censor. Not bullet-proof.
/// </summary>
public sealed class SimpleCensor : IChatCensor
{
    // Common substitution symbols are replaced with one of the characters they commonly substitute.
    private bool _shouldSanitizeLeetspeak;
    private FrozenDictionary<char, char> _leetspeakReplacements = FrozenDictionary<char, char>.Empty;

    // Special characters are replaced with spaces.
    private bool _shouldSanitizeSpecialCharacters;
    private HashSet<char> _specialCharacterReplacements = [];

    // Censored words are removed unless they're a false positive (e.g. Scunthorpe)
    private string[] _censoredWords = Array.Empty<string>();
    private string[] _falsePositives = Array.Empty<string>();

    // False negatives are censored words that contain a false positives.
    private string[] _falseNegatives = Array.Empty<string>();

    // What unicode ranges are allowed? If this array is empty, don't filter by range.
    private UnicodeRange[] _allowedUnicodeRanges= Array.Empty<UnicodeRange>();

    /// <summary>
    /// Censors the input string.
    /// </summary>
    /// <param name="input">The input string</param>
    /// <param name="output">The output string</param>
    /// <param name="replaceWith">The character to replace with</param>
    /// <returns>If output is valid</returns>
    public bool Censor(string input, out string output, char replaceWith = '*')
    {
        output = Censor(input, replaceWith);

        return !string.Equals(input, output);
    }

    public string Censor(string input, char replaceWith = '*')
    {
        // We flat-out ban anything not in the allowed unicode ranges, stripping them
        input = SanitizeOutBlockedUnicode(input);

        var originalInput = input.ToCharArray();

        input = SanitizeInput(input);

        var censored = input.ToList();

        // Remove false negatives
        input = CheckProfanity(input, censored, _falseNegatives, replaceWith);

        // Get false positives
        var falsePositives = FindFalsePositives(censored, replaceWith);

        // Remove censored words
        CheckProfanity(input, censored, _censoredWords, replaceWith);

        // Reconstruct
        // Reconstruct false positives
        for (var i = 0; i < falsePositives.Length; i++)
        {
            if (falsePositives[i] != replaceWith)
            {
                censored[i] = falsePositives[i];
            }
        }

        for (var i = 0; i < originalInput.Length; i++)
        {
            if (originalInput[i] == ' ')
            {
                censored.Insert(i, ' ');

                continue;
            }

            if (_shouldSanitizeSpecialCharacters && _specialCharacterReplacements.Contains(originalInput[i]))
            {
                censored.Insert(i, originalInput[i]);

                continue;
            }

            if (_shouldSanitizeLeetspeak || _shouldSanitizeSpecialCharacters)
            {
                // detect "()"
                if (originalInput[i] == '(' && i != originalInput.Length - 1 && originalInput[i+1] == ')')
                {
                    // censored has now had "o" replaced with "o) so both strings line up again..."
                    censored.Insert(i+1, censored[i] != replaceWith ? ')' : replaceWith);
                }
            }

            if (censored[i] != replaceWith)
            {
                censored[i] = originalInput[i];
            }
        }

        // SO says this is fast...
        return string.Concat(censored);
    }

    /// <summary>
    /// Adds a l33tsp34k sanitization rule
    /// </summary>
    /// <returns>The censor for further configuration</returns>
    public SimpleCensor WithSanitizeLeetSpeak()
    {
        _shouldSanitizeLeetspeak = true;

        return BuildCharacterReplacements();
    }

    /// <summary>
    /// Adds a l33tsp34k sanitization rule
    /// </summary>
    /// <returns>The censor for further configuration</returns>
    public SimpleCensor WithSanitizeSpecialCharacters()
    {
        _shouldSanitizeSpecialCharacters = true;

        return BuildCharacterReplacements();
    }

    public SimpleCensor WithRanges(UnicodeRange[] ranges)
    {
        _allowedUnicodeRanges = ranges;

        return this;
    }

    public SimpleCensor WithCustomDictionary(string[] naughtyWords)
    {
        _censoredWords = naughtyWords;

        return this;
    }

    public SimpleCensor WithFalsePositives(string[] falsePositives)
    {
        _falsePositives = falsePositives;

        return this;
    }

    public SimpleCensor WithFalseNegatives(string[] falseNegatives)
    {
        _falseNegatives = falseNegatives;

        return this;
    }

    public SimpleCensor WithLeetspeakReplacements(Dictionary<char, char> replacements)
    {
        _leetspeakReplacements = replacements.ToFrozenDictionary();

        return this;
    }

    public SimpleCensor WithSpecialCharacterReplacements(Dictionary<char, char> replacements)
    {
        _leetspeakReplacements = replacements.ToFrozenDictionary();

        return this;
    }

    private string CheckProfanity(string input, List<char> censored, string[] words, char replaceWith = '*')
    {
        foreach (var word in words)
        {
            var wordLength = word.Length;
            var endOfFoundWord = 0;
            var foundIndex = input.IndexOf(word, endOfFoundWord, StringComparison.OrdinalIgnoreCase);

            while(foundIndex > -1)
            {
                endOfFoundWord = foundIndex + wordLength;

                for (var i = 0; i < wordLength; i++)
                {
                    censored[foundIndex+i] = replaceWith;
                }

                foundIndex = input.IndexOf(word, endOfFoundWord, StringComparison.OrdinalIgnoreCase);
            }
        }

        return input;
    }

    private char[] FindFalsePositives(List<char> chars, char replaceWith = '*')
    {
        var input = string.Concat(chars);

        var output = Enumerable.Repeat(replaceWith, input.Length).ToArray();
        var inputAsARr = input.ToArray();

        foreach (var word in _falsePositives)
        {
            var wordLength = word.Length;
            var endOfFoundWord = 0;
            var foundIndex = input.IndexOf(word, endOfFoundWord, StringComparison.OrdinalIgnoreCase);

            while(foundIndex > -1)
            {
                endOfFoundWord = foundIndex + wordLength;

                for (var i = foundIndex; i < endOfFoundWord; i++)
                {
                    output[i] = inputAsARr[i];
                }

                foundIndex = input.IndexOf(word, endOfFoundWord, StringComparison.OrdinalIgnoreCase);
            }
        }

        return output;
    }

    private string SanitizeInput(string input)
    {
        // "()" is a broad enough trick to beat censors that we we should check for it broadly.
        if (_shouldSanitizeLeetspeak || _shouldSanitizeSpecialCharacters)
        {
            input = input.Replace("()", "o");
        }

        var sb = new StringBuilder();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var character in input)
        {
            if (character == ' ' || _shouldSanitizeSpecialCharacters && _specialCharacterReplacements.Contains(character))
            {
                continue;
            }

            if (_shouldSanitizeLeetspeak && _leetspeakReplacements.TryGetValue(character, out var leetRepl))
            {
                sb.Append(leetRepl);

                continue;
            }

            sb.Append(character);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns a string with all characters not in ISO-8851-1 replaced with question marks
    /// </summary>
    private string SanitizeOutBlockedUnicode(string input)
    {
        if (_allowedUnicodeRanges.Length <= 0)
        {
            return input;
        }

        var sb = new StringBuilder();

        foreach (var symbol in input.EnumerateRunes())
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var range in _allowedUnicodeRanges)
            {
                if (symbol.Value < range.FirstCodePoint || symbol.Value >= range.FirstCodePoint + range.Length)
                    continue;

                sb.Append(symbol);

                break;
            }
        }

        return sb.ToString();
    }

    private SimpleCensor BuildCharacterReplacements()
    {
        if (_shouldSanitizeSpecialCharacters)
        {
            _specialCharacterReplacements =
            [
                '-',
                '_',
                '|',
                '.',
                ',',
                '(',
                ')',
                '<',
                '>',
                '"',
                '`',
                '~',
                '*',
                '&',
                '%',
                '$',
                '#',
                '@',
                '!',
                '?',
                '+'
            ];
        }

        if (_shouldSanitizeLeetspeak)
        {
            _leetspeakReplacements = new Dictionary<char, char>
            {
                ['4'] = 'a',
                ['$'] = 's',
                ['!'] = 'i',
                ['+'] = 't',
                ['#'] = 'h',
                ['@'] = 'a',
                ['0'] = 'o',
                ['1'] = 'i', // also obviously can be l; gamer-words need i's more though.
                ['7'] = 'l',
                ['3'] = 'e',
                ['5'] = 's',
                ['9'] = 'g',
                ['<'] = 'c'
            }.ToFrozenDictionary();
        }

        return this;
    }
}
