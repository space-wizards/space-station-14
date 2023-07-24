using System.Linq;
using Content.Server.NewCon.Errors;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Content.Server.NewCon;

public interface IInvocationContext
{
    ICommonSession? Session { get; }

    public void WriteLine(string line);

    public void WriteLine(FormattedMessage line)
    {
        // Cut markup for server.
        if (Session is null)
        {
            WriteLine(line.ToString());
            return;
        }

        WriteLine(line.ToMarkup());
    }

    public void WriteMarkup(string markup)
    {
        WriteLine(FormattedMessage.FromMarkup(markup));
    }

    public void WriteError(IConError error)
    {
        WriteLine(error.Describe());
    }

    public void ReportError(IConError err);

    public IEnumerable<IConError> GetErrors();

    public void ClearErrors();
}
