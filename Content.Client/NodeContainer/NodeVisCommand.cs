using Robust.Shared.Console;

namespace Content.Client.NodeContainer;

public sealed partial class NodeVisCommand : LocalizedEntityCommands
{
    [Dependency] private NodeGroupVisualsSystem _nodeSystem = default!;

    public override string Command => "nodevisfilter";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            foreach (var filtered in _nodeSystem.Filtered)
            {
                shell.WriteLine(filtered);
            }
        }
        else
        {
            var filter = args[0];
            if (!_nodeSystem.Filtered.Add(filter))
                _nodeSystem.Filtered.Remove(filter);
        }
    }
}
