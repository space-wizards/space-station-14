using Content.Client.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.NodeContainer
{
    public sealed class NodeVisCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _e = default!;

        public string Command => "nodevis";
        public string Description => "Toggles node group visualization";
        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var adminMan = IoCManager.Resolve<IClientAdminManager>();
            if (!adminMan.HasFlag(AdminFlags.Debug))
            {
                shell.WriteError("You need +DEBUG for this command");
                return;
            }

            var sys = _e.System<NodeGroupSystem>();
            sys.SetVisEnabled(!sys.VisEnabled);
        }
    }

    public sealed class NodeVisFilterCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _e = default!;

        public string Command => "nodevisfilter";
        public string Description => "Toggles showing a specific group on nodevis";
        public string Help => "Usage: nodevis [filter]\nOmit filter to list currently masked-off";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var sys = _e.System<NodeGroupSystem>();

            if (args.Length == 0)
            {
                foreach (var filtered in sys.Filtered)
                {
                    shell.WriteLine(filtered);
                }
            }
            else
            {
                var filter = args[0];
                if (!sys.Filtered.Add(filter))
                {
                    sys.Filtered.Remove(filter);
                }
            }
        }
    }
}
