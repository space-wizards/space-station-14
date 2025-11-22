using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Console;

namespace Content.Server.Xenoarchaeology.Artifact;

/// <summary> Command for unlocking a specific node of a xeno artifact. </summary>
[AdminCommand(AdminFlags.Debug)]
public sealed class UnlockNodeCommand : XenoArtifactCommandBase
{
    public override string Command => "unlocknode";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!EntityUid.TryParse(args[1], out var entityUid))
        {
            shell.WriteError(Loc.GetString("shell-could-not-find-entity-with-uid", ("uid", args[1])));
            return;
        }

        Artifact.SetNodeUnlocked(entityUid);
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
