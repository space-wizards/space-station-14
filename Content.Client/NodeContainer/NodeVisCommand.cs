using Content.Client.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Client.NodeContainer
{
    public sealed class NodeVisCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IClientAdminManager _admin = default!;
        [Dependency] private readonly NodeGroupSystem _nodes = default!;

        public override string Command => "nodevis";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!_admin.HasFlag(AdminFlags.Debug))
                shell.WriteError(Loc.GetString($"cmd-nodevis-error"));
            else
                _nodes.SetVisEnabled(!_nodes.VisEnabled);
        }
    }

    public sealed class NodeVisFilterCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly NodeGroupSystem _nodes = default!;

        public override string Command => "nodevisfilter";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length == 0)
            {
                foreach (var filtered in _nodes.Filtered)
                {
                    shell.WriteLine(filtered);
                }
            }
            else
            {
                var filter = args[0];
                if (!_nodes.Filtered.Add(filter))
                {
                    _nodes.Filtered.Remove(filter);
                }
            }
        }
    }
}
