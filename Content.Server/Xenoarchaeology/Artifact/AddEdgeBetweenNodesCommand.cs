using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Console;

namespace Content.Server.Xenoarchaeology.Artifact;

[AdminCommand(AdminFlags.Debug)]
public sealed class AddEdgeBetweenNodesCommand : XenoArtifactCommandBase
{
    /// <inheritdoc />
    public override string Command => "addedgebetweennodes";

    /// <inheritdoc />
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if(!EntityUid.TryParse(args[0], out var artifactEnt))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-common-failed-to-find-artifact", ("uid", args[0])));
            return;
        }

        if (!EntityUid.TryParse(args[1], out var nodeEntFrom)
            || !EntityManager.TryGetComponent(nodeEntFrom, out XenoArtifactNodeComponent? _))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-common-failed-to-find-node", ("uid", args[1])));
            return;
        }

        if (!EntityUid.TryParse(args[2], out var nodeEntTo)
            || !EntityManager.TryGetComponent(nodeEntTo, out XenoArtifactNodeComponent? _))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-common-failed-to-find-node", ("uid", args[2])));
            return;
        }

        Artifact.AddEdge(artifactEnt, nodeEntFrom, nodeEntTo);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 =>
                CompletionResult.FromHintOptions(
                    ContentCompletionHelper.ByComponentAndEntityUid<XenoArtifactComponent>(args[0], EntityManager),
                    Loc.GetString("cmd-xenoartifact-common-artifact-hint")
                ),
            2 when EntityUid.TryParse(args[0], out var artifactUid) =>
                CompletionResult.FromHintOptions(
                    GetNodes(args[1], artifactUid),
                    Loc.GetString("cmd-xenoartifact-common-node-hint")
                ),
            3 when EntityUid.TryParse(args[0], out var artifactUid) && EntityUid.TryParse(args[1], out var otherNode) =>
                CompletionResult.FromHintOptions(
                    GetNodes(args[2], artifactUid, except: otherNode),
                    Loc.GetString("cmd-xenoartifact-common-node-hint")
                ),
            _ => CompletionResult.Empty
        };
    }
}
