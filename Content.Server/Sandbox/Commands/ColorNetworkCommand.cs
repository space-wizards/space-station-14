using Content.Server.Administration.Managers;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Sandbox.Commands
{
    [AnyCommand]
    public sealed class ColorNetworkCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public override string Command => "colornetwork";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var sandboxManager = _entManager.System<SandboxSystem>();
            var adminManager = IoCManager.Resolve<IAdminManager>();
            if (shell.IsClient && (!sandboxManager.IsSandboxEnabled && !adminManager.HasAdminFlag(shell.Player!, AdminFlags.Mapping)))
            {
                shell.WriteError(Loc.GetString("cmd-colornetwork-no-access"));
            }

            if (args.Length != 3)
            {
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!int.TryParse(args[0], out var targetId))
            {
                shell.WriteLine(Loc.GetString("shell-argument-must-be-number"));
                return;
            }

            var nent = new NetEntity(targetId);

            if (!_entManager.TryGetEntity(nent, out var eUid))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            if (!_entManager.TryGetComponent(eUid, out NodeContainerComponent? nodeContainerComponent))
            {
                shell.WriteLine(Loc.GetString("shell-entity-is-not-node-container"));
                return;
            }

            if (!Enum.TryParse(args[1], out NodeGroupID nodeGroupId))
            {
                shell.WriteLine(Loc.GetString("shell-node-group-is-invalid"));
                return;
            }

            var color = Color.TryFromHex(args[2]);
            if (!color.HasValue)
            {
                shell.WriteError(Loc.GetString("shell-invalid-color-hex"));
                return;
            }

            PaintNodes(nodeContainerComponent, nodeGroupId, color.Value);
        }

        private void PaintNodes(NodeContainerComponent nodeContainerComponent, NodeGroupID nodeGroupId, Color color)
        {
            var group = nodeContainerComponent.Nodes[nodeGroupId.ToString().ToLower()].NodeGroup;

            if (group == null)
                return;

            foreach (var x in group.Nodes)
            {
                if (!_entManager.TryGetComponent(x.Owner, out AtmosPipeColorComponent? atmosPipeColorComponent))
                    continue;

                _entManager.System<AtmosPipeColorSystem>().SetColor(x.Owner, atmosPipeColorComponent, color);
            }
        }
    }
}
