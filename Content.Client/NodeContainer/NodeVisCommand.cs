using Content.Client.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Client.NodeContainer
{
    public sealed class NodeVisCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly NodeGroupSystem _nodeSystem = default!;

        public override string Command => "nodevis";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!_adminManager.HasFlag(AdminFlags.Debug))
            {
                shell.WriteError(Loc.GetString($"shell-missing-required-permission", ("perm", "+DEBUG")));
                return;
            }

            _nodeSystem.SetVisEnabled(!_nodeSystem.VisEnabled);
        }
    }

    public sealed class NodeVisFilterCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly NodeGroupSystem _nodeSystem = default!;

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
}
