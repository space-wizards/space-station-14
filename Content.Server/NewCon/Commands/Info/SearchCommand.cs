using System.Linq;
using Content.Server.NewCon.Errors;
using Robust.Shared.Utility;

namespace Content.Server.NewCon.Commands.Info;

[ConsoleCommand]
public sealed class SearchCommand : ConsoleCommand
{
    [CommandImplementation]
    public IEnumerable<FormattedMessage> Search<T>([PipedArgument] IEnumerable<T> input, [CommandArgument] string term)
    {
        var list = input.Select(x => x!.ToString()!).ToList();
        return list.Where(x => x.Contains(term, StringComparison.InvariantCultureIgnoreCase)).Select(x =>
        {
            var startIdx = x.IndexOf(term, StringComparison.InvariantCultureIgnoreCase);
            return ConHelpers.HighlightSpan(x, (startIdx, startIdx + term.Length), Color.Aqua);
        });
    }
}
