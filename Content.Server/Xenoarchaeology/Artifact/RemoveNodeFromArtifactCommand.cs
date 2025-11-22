using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Console;

namespace Content.Server.Xenoarchaeology.Artifact;

[AdminCommand(AdminFlags.Debug)]
public sealed class RemoveNodeFromArtifactCommand : XenoArtifactCommandBase
{
    /// <inheritdoc />
    public override string Command => "removenodefromartifact";

    /// <inheritdoc />
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var target)
            || !EntityManager.TryGetComponent(target, out XenoArtifactComponent? artifactComp))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-common-failed-to-find-artifact", ("uid", args[0])));
            return;
        }

        if (!EntityUid.TryParse(args[1], out var toDelete)
            || !EntityManager.TryGetComponent(toDelete, out XenoArtifactNodeComponent? nodeComponent))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-common-failed-to-find-node", ("uid", args[0])));
            return;
        }

        Artifact.RemoveNode((target, artifactComp), (toDelete, nodeComponent));
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
            _ => CompletionResult.Empty
        };
    }
}
