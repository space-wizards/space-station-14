using System.Linq;
using System.Text;
using Content.Shared.Decals;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Shared.Chat.V2;

public partial class SharedChatSystem
{
    [ValidatePrototypeId<ColorPalettePrototype>]
    private const string ChatNamePalette = "Material";

    [ValidatePrototypeId<SpeechVerbPrototype>]
    public const string DefaultSpeechVerb = "Default";

    private string[] _chatNameColors = default!;

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

    public static string CapitalizeFirstLetter(string message)
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
