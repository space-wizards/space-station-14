using System.Linq;
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
}
