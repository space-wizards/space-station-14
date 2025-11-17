using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Console;

namespace Content.Server.Xenoarchaeology.Artifact;

[AdminCommand(AdminFlags.Debug)]
public sealed class RemoveNodeFromArtifactCommand : LocalizedEntityCommands
{
    [Dependency] private readonly XenoArtifactSystem _artifact = default!;

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

        if (!NetEntity.TryParse(args[0], out var artifactId)
            || !EntityManager.TryGetEntity(artifactId, out var target)
            || !EntityManager.TryGetComponent(target.Value, out XenoArtifactComponent? artifactComp))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-common-failed-to-find-artifact", ("uid", args[0])));
            return;
        }

        if (!NetEntity.TryParse(args[1], out var toDeleteNetEnt)
            || !EntityManager.TryGetEntity(toDeleteNetEnt, out var toDelete)
            || !EntityManager.TryGetComponent(toDelete, out XenoArtifactNodeComponent? nodeComponent))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-common-failed-to-find-node", ("uid", args[0])));
            return;
        }

        _artifact.RemoveNode((target.Value, artifactComp), (toDelete.Value, nodeComponent));

        _artifact.RebuildXenoArtifactMetaData((target.Value, artifactComp));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
            {
                var query = EntityManager.EntityQueryEnumerator<XenoArtifactComponent>();
                var completionOptions = new List<CompletionOption>();
                while (query.MoveNext(out var uid, out _))
                {
                    var netEntity = EntityManager.GetNetEntity(uid);
                    completionOptions.Add(new CompletionOption(netEntity.Id.ToString()));
                }
                return CompletionResult.FromHintOptions(completionOptions, Loc.GetString("cmd-xenoartifact-common-artifact-hint"));
            }
            case 2:
            {
                var query = EntityManager.EntityQueryEnumerator<XenoArtifactNodeComponent>();
                if (!int.TryParse(args[0], out var artifactEntId))
                {
                    return CompletionResult.Empty;
                }

                var completionOptions = new List<CompletionOption>();
                while (query.MoveNext(out var uid, out var nodeComp))
                {
                    if (nodeComp.Attached?.Id == artifactEntId)
                    {
                        var netEntity = EntityManager.GetNetEntity(uid);
                        completionOptions.Add(new CompletionOption(netEntity.Id.ToString(), nodeComp.TriggerTip));
                    }
                }

                return CompletionResult.FromHintOptions(completionOptions, Loc.GetString("cmd-xenoartifact-common-node-hint"));
            }
            default:
                return CompletionResult.Empty;
        }
    }
}
