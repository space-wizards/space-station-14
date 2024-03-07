using System.Collections.Frozen;
using System.Linq;
using System.Text;
using System.Text.Unicode;

namespace Content.Server.Chat.V2.Moderation;

/// <summary>
/// A basic censor. Mostly built from reference from Go-Away. Not bullet-proof.
/// </summary>
public sealed class SimpleCensor
{
    private bool _sanitizeSpecialCharacters;
    private bool _sanitizeL33TSpeak;
    private bool _sanitizeSpaces;

    private FrozenDictionary<char, char> _characterReplacements = new Dictionary<char, char>().ToFrozenDictionary();

    private List<string> _censoredWords = new();
    private List<string> _falsePositives = new();
    private List<string> _falseNegatives = new();
    private HashSet<UnicodeRange> _unicodeRanges = new();

    public bool Censor(string input, out string output, char replaceWith = '*')
    {
        output = Censor(input, replaceWith);

        return !string.IsNullOrEmpty(output);
    }

    public SimpleCensor WithSanitizeL33TSpeak()
    {
        _sanitizeL33TSpeak = true;

        return BuildCharacterReplacements();
    }

    public SimpleCensor WithSanitizeSpecialCharacters()
    {
        _sanitizeSpecialCharacters = true;

        return BuildCharacterReplacements();
    }

    public SimpleCensor WithRanges(HashSet<UnicodeRange> ranges)
    {
        _unicodeRanges = ranges;

        return this;
    }

    public SimpleCensor WithSanitizeSpaces()
    {
        _sanitizeSpaces = true;

        return this;
    }

    public SimpleCensor WithCustomDictionary(List<string> naughtyWords, List<string>? falsePositives, List<string>? falseNegatives)
    {
        _censoredWords = naughtyWords;

        if (falsePositives != null)
            _falsePositives = falsePositives;

        if (falseNegatives != null)
            _falseNegatives = falseNegatives;

        return this;
    }

    public SimpleCensor WithCustomCharacterReplacements(Dictionary<char, char> replacements)
    {
        _characterReplacements = replacements.ToFrozenDictionary();

        return this;
    }

    public string Censor(string input, char replaceWith = '*')
    {
        input = _unicodeRanges.Count > 0 ? ApplyUnicodeRangeAllowList(input) : input;

        var censored = input.ToCharArray();

        input = Sanitize(input, out var originalIndexes, true);
        CheckProfanity(ref input, ref originalIndexes, ref censored, _falseNegatives, replaceWith);
        RemoveFalsePositives(ref input, ref originalIndexes);
        CheckProfanity(ref input, ref originalIndexes, ref censored, _censoredWords, replaceWith);

        return new string(censored);
    }

    private void CheckProfanity(ref string input, ref List<int> originalIndexes, ref char[] censored, List<string> words, char replaceWith = '*')
    {
        foreach (var word in words)
        {
            var wordLength = word.Length;
            var currentIndex = 0;

            for (var foundIndex = input[currentIndex..].IndexOf(word, StringComparison.OrdinalIgnoreCase); foundIndex > -1; foundIndex = input[currentIndex..].IndexOf(word, StringComparison.OrdinalIgnoreCase))
            {
                for (var j = 0; j < wordLength; j++)
                {
                    var runeIdx = IndexToRune(input, currentIndex + foundIndex) + j;
                    if (runeIdx < originalIndexes.Count)
                    {
                        censored[originalIndexes[runeIdx]] = replaceWith;
                    }
                }

                currentIndex += foundIndex + wordLength;
            }
        }
    }

    private void RemoveFalsePositives(ref string input, ref List<int> originalIndexes)
    {
        foreach (var word in _falsePositives)
        {
            var wordLength = word.Length;
            var currentIndex = 0;

            for (var foundIndex = input[currentIndex..].IndexOf(word, StringComparison.OrdinalIgnoreCase); foundIndex > -1; foundIndex = input[currentIndex..].IndexOf(word, StringComparison.OrdinalIgnoreCase))
            {
                // shufflin'
                var foundRuneIndex = IndexToRune(input, foundIndex);
                var newOriginalIndexes = new List<int>(originalIndexes[..foundRuneIndex]);
                newOriginalIndexes.AddRange(originalIndexes[(foundRuneIndex + wordLength)..]);
                originalIndexes = newOriginalIndexes;

                currentIndex += foundIndex + wordLength;
            }

            input = input.Replace(word, "");
        }
    }

    private string Sanitize(string input, out List<int> originalIndexes, bool rememberOriginalIndexes)
    {
        input = input.ToLower();
        if (_sanitizeL33TSpeak && _sanitizeSpecialCharacters)
        {
            // catch one specific trick
            input = input.Replace("()", "o");
        }

        foreach (var rune in input)
        {
            if (!_characterReplacements.TryGetValue(rune, out var repl))
                continue;

            if (_sanitizeSpecialCharacters && repl == ' ')
            {
                input = input.Replace(rune, ' ');
            }
            else if (_sanitizeL33TSpeak && repl != ' ')
            {
                input = input.Replace(rune, repl);
            }
        }

        originalIndexes = new List<int>();
        if (rememberOriginalIndexes)
        {
            var i = 0;

            foreach (var rune in input)
            {
                if (rune != ' ' || !_sanitizeSpaces)
                {
                    originalIndexes.Add(i);
                }

                i++;
            }
        }

        if (_sanitizeSpaces)
        {
            input = input.Replace(" ", "");
        }

        return input;
    }

    /// <summary>
    /// Returns a string with all characters not in ISO-8851-1 replaced with question marks
    /// </summary>
    private string ApplyUnicodeRangeAllowList(string input)
    {
        var sb = new StringBuilder();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator // Runes are funky
        foreach (Rune symbol in input)
        {
            if (_unicodeRanges.Any(range => Enumerable.Range(range.FirstCodePoint, range.FirstCodePoint + range.Length).Contains(symbol.Value)))
                sb.Append(symbol);
        }

        return sb.ToString();
    }

    private SimpleCensor BuildCharacterReplacements()
    {
        var replacements = new Dictionary<char, char>();
        if (_sanitizeSpecialCharacters)
        {
            replacements['-'] = ' ';
            replacements['_'] = ' ';
            replacements['|'] = ' ';
            replacements['.'] = ' ';
            replacements[','] = ' ';
            replacements['('] = ' ';
            replacements[')'] = ' ';
            replacements['<'] = ' ';
            replacements['>'] = ' ';
            replacements['"'] = ' ';
            replacements['`'] = ' ';
            replacements['~'] = ' ';
            replacements['*'] = ' ';
            replacements['&'] = ' ';
            replacements['%'] = ' ';
            replacements['$'] = ' ';
            replacements['#'] = ' ';
            replacements['@'] = ' ';
            replacements['!'] = ' ';
            replacements['?'] = ' ';
            replacements['+'] = ' ';
        }

        if (_sanitizeL33TSpeak)
        {
            replacements['4'] = 'a';
            replacements['$'] = 's';
            replacements['!'] = 'i';
            replacements['+'] = 't';
            replacements['#'] = 'h';
            replacements['@'] = 'a';
            replacements['0'] = 'o';
            replacements['1'] = 'i'; // also obviously can be l; gamer-words need i's more though.
            replacements['7'] = 'l';
            replacements['3'] = 'e';
            replacements['5'] = 's';
            replacements['9'] = 'g';
            replacements['<'] = 'c';
        }

        _characterReplacements = replacements.ToFrozenDictionary();

        return this;
    }

    private int IndexToRune(string s, int index)
    {
        var count = 0;

        for (var i = 0; i < s.Length; i++)
        {
            if (i == index)
            {
                break;
            }

            if (i <= index)
            {
                count++;
            }
        }

        return count;
    }
}
