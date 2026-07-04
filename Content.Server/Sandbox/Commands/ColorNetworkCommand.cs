using Content.Server.Administration.Managers;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.NodeContainer.Components;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Sandbox.Commands;

[AnyCommand]
public sealed partial class ColorNetworkCommand : LocalizedEntityCommands
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private AtmosPipeColorSystem _pipeColorSystem = default!;
    [Dependency] private SandboxSystem _sandboxSystem = default!;

    public override string Command => "colornetwork";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.IsClient || (!_sandboxSystem.IsSandboxEnabled && !_adminManager.HasAdminFlag(shell.Player!, AdminFlags.Mapping)))
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

        if (!EntityManager.TryGetEntity(nent, out var eUid))
        {
            shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
            return;
        }

        if (!EntityManager.TryGetComponent(eUid, out NodeContainerComponent? nodeContainerComponent))
        {
            shell.WriteLine(Loc.GetString("shell-entity-is-not-node-container"));
            return;
        }

        var color = Color.TryFromHex(args[2]);
        if (!color.HasValue)
        {
            shell.WriteError(Loc.GetString("shell-invalid-color-hex"));
            return;
        }

        PaintNodes(nodeContainerComponent, args[1], color.Value);
    }

    private void PaintNodes(NodeContainerComponent nodeContainerComponent, string nodeName, Color color)
    {
        var group = nodeContainerComponent.Nodes[nodeName].NodeGroup;

        if (group == null)
            return;

        foreach (var x in group.Value.Comp.Nodes)
        {
            if (!EntityManager.TryGetComponent(x.Owner, out AtmosPipeColorComponent? atmosPipeColorComponent))
                continue;

            _pipeColorSystem.SetColor(x.Owner, atmosPipeColorComponent, color);
        }
    }
}
