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
            WriteLine(line.Nodes.Where(x => x.Name is null).Select(x => x.Value.StringValue!).Aggregate((x, y) => x + y));
            return;
        }

        WriteLine(line.ToString());
    }
}
