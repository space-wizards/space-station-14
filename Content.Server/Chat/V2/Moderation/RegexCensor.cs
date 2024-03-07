using System.Text.RegularExpressions;

namespace Content.Server.Chat.V2.Moderation;

public sealed class RegexCensor
{
    private Regex _censorInstruction;

    public RegexCensor(Regex censorInstruction)
    {
        _censorInstruction = censorInstruction;
    }

    public bool Censor(string input, out string output, char replaceWith = '*')
    {
        output = _censorInstruction.Replace(input, replaceWith.ToString());

        return !string.IsNullOrEmpty(output);
    }
}
