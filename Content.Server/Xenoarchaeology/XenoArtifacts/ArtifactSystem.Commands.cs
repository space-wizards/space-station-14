using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public partial class ArtifactSystem
{
    [Dependency] private readonly IConsoleHost _conHost = default!;

    public void InitializeCommands()
    {
        _conHost.RegisterCommand("forceartifactnode", "Forces an artifact to traverse to a given node", "forceartifacteffect <uid> <node ID>",
            ForceArtifactNode,
            ForceArtifactNodeCompletions);

        _conHost.RegisterCommand("getartifactmaxvalue", "Reports the maximum research point value for a given artifact", "forceartifacteffect <uid>",
            GetArtifactMaxValue);
    }

    [AdminCommand(AdminFlags.Fun)]
    private void ForceArtifactNode(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 2)
            shell.WriteError("Argument length must be 2");

        if (!NetEntity.TryParse(args[0], out var uidNet) || !TryGetEntity(uidNet, out var uid) || !int.TryParse(args[1], out var id))
            return;

        if (!TryComp<ArtifactComponent>(uid, out var artifact))
            return;

        if (artifact.NodeTree.FirstOrDefault(n => n.Id == id) is { } node)
        {
            EnterNode(uid.Value, ref node);
        }
    }

    private CompletionResult ForceArtifactNodeCompletions(IConsoleShell shell, string[] args)
    {
        if (args.Length == 2 && NetEntity.TryParse(args[0], out var uidNet) && TryGetEntity(uidNet, out var uid))
        {
            if (TryComp<ArtifactComponent>(uid, out var artifact))
            {
                return CompletionResult.FromHintOptions(artifact.NodeTree.Select(s => s.Id.ToString()), "<node id>");
            }
        }

        return CompletionResult.Empty;
    }

    [AdminCommand(AdminFlags.Debug)]
    private void GetArtifactMaxValue(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 1)
            shell.WriteError("Argument length must be 1");

        if (!NetEntity.TryParse(args[0], out var uidNet) || !TryGetEntity(uidNet, out var uid))
            return;

        if (!TryComp<ArtifactComponent>(uid, out var artifact))
            return;

        var pointSum = GetResearchPointValue(uid.Value, artifact, true);
        shell.WriteLine($"Max point value for {ToPrettyString(uid.Value)} with {artifact.NodeTree.Count} nodes: {pointSum}");
    }
}
