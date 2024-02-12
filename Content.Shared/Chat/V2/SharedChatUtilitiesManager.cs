using System.Linq;
using System.Text;
using Content.Shared.Decals;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Chat.V2;

/// <summary>
/// Contains common utilities used across various chat systems
/// </summary>
public interface IChatUtilities
{
    public void Initialize();

    /// <summary>
    /// Get a consistent chat color for a given name.
    /// </summary>
    /// <param name="name">The name to get the color for</param>
    /// <returns>A color. It will always be the same for a given string.</returns>
    public string GetNameColor(string name);

    /// <summary>
    /// Obfuscate a message with a given chance of each character being visible.
    /// </summary>
    /// <param name="message">The message to obfuscate.</param>
    /// <param name="chance">The decimal chance (0-1 bounded) that a character is not obfuscated.</param>
    /// <returns>The obfuscated message.</returns>
    public string ObfuscateMessageReadability(string message, float chance);

    /// <summary>
    /// Get an appropriate speech verb for a provided entity and message.
    /// </summary>
    /// <param name="source">The speaking entity.</param>
    /// <param name="message">The message they want to say.</param>
    /// <returns>An appropriate speech verb prototype.</returns>
    public SpeechVerbPrototype GetSpeechVerb(EntityUid source, string message);

    /// <summary>
    /// Add a period to the message.
    /// </summary>
    /// <param name="message">The message to add a period to.</param>
    /// <returns>The message, plus a period.</returns>
    public string AddAPeriod(string message);

    /// <summary>
    /// Make sure the I pronoun is capitalized in a message.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <returns>The formatted message.</returns>
    public string CapitalizeIPronoun(string message);

    /// <summary>
    /// Make sure the first letter is capitalized.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <returns>The formatted message.</returns>
    public string CapitalizeFirstLetter(string message);

    public string BuildGibberishString(IReadOnlyList<char> charOptions, int length);
}

public sealed class SharedChatUtilitiesManager : IChatUtilities
{
    [ValidatePrototypeId<ColorPalettePrototype>]
    private const string ChatNamePalette = "Material";

    [ValidatePrototypeId<SpeechVerbPrototype>]
    public const string DefaultSpeechVerb = "Default";

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private string[] _chatNameColors = default!;

    public void Initialize()
    {
        var nameColors = _prototype.Index<ColorPalettePrototype>(ChatNamePalette).Colors.Values.ToArray();
        _chatNameColors = new string[nameColors.Length];
        for (var i = 0; i < nameColors.Length; i++)
        {
            _chatNameColors[i] = nameColors[i].ToHex();
        }
    }

    public string GetNameColor(string name)
    {
        var colorIdx = Math.Abs(name.GetHashCode() % _chatNameColors.Length);

        return _chatNameColors[colorIdx];
    }

    public string ObfuscateMessageReadability(string message, float chance)
    {
        var obfuscatedMessage = message.ToCharArray();

        for (var i = 0; i < message.Length; i++)
        {
            if (char.IsWhiteSpace((obfuscatedMessage[i])))
            {
                continue;
            }

            if (_random.Prob(1 - chance))
            {
                obfuscatedMessage[i] = '~';
            }
        }

        return new string(obfuscatedMessage) ?? "";
    }

    public SpeechVerbPrototype GetSpeechVerb(EntityUid source, string message)
    {
        if (!_entity.TryGetComponent<SpeechComponent>(source, out var speech))
            return _prototypeManager.Index<SpeechVerbPrototype>(DefaultSpeechVerb);

        // check for a suffix-applicable speech verb
        SpeechVerbPrototype? current = null;
        foreach (var (str, id) in speech.SuffixSpeechVerbs)
        {
            var proto = _prototypeManager.Index(id);
            if (message.EndsWith(Loc.GetString(str)) && proto.Priority >= (current?.Priority ?? 0))
            {
                current = proto;
            }
        }

        // if no applicable suffix verb return the normal one used by the entity
        return current ?? _prototypeManager.Index(speech.SpeechVerb);
    }

    public string AddAPeriod(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        if (char.IsLetter(message[^1]))
            message += ".";

        return message;
    }

    public string CapitalizeIPronoun(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        for
        (
            var index = message.IndexOf('i');
            index != -1;
            index = message.IndexOf('i', index + 1)
        )
        {
            // Stops the code If It's tryIng to capItalIze the letter I In the middle of words
            // Repeating the code twice is the simplest option

            // TODO: there is probably some regex bullshit you can do here instead
            if (index + 1 < message.Length && char.IsLetter(message[index + 1]))
                continue;
            if (index - 1 >= 0 && char.IsLetter(message[index - 1]))
                continue;

            var beforeTarget = message.Substring(0, index);
            var target = message.Substring(index, 1);
            var afterTarget = message.Substring(index + 1);

            message = beforeTarget + target.ToUpper() + afterTarget;
        }

        return message;
    }

    public string CapitalizeFirstLetter(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        message = char.ToUpper(message[0]) + message.Remove(0, 1);
        return message;
    }

    public string BuildGibberishString(IReadOnlyList<char> charOptions, int length)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            sb.Append(_random.Pick(charOptions));
        }
        return sb.ToString();
    }
}
