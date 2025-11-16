using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Console;

namespace Content.Server.Xenoarchaeology.Artifact;

[AdminCommand(AdminFlags.Debug)]
public sealed class AddEdgeBetweenArtifactNodesCommand : LocalizedEntityCommands
{
    [Dependency] private readonly XenoArtifactSystem _artifact = default!;

    /// <inheritdoc />
    public override string Command => "addedgebetweeennodes";

    /// <inheritdoc />
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if(!NetEntity.TryParse(args[0], out var artifactNetEnt)
           || !EntityManager.TryGetEntity(artifactNetEnt, out var artifactEnt))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-commands-failed-to-find-artifact", ("uid", args[0])));
            return;
        }

        if (!NetEntity.TryParse(args[1], out var nodeNetEntFrom)
            || !EntityManager.TryGetEntity(nodeNetEntFrom, out var nodeEntFrom)
            || !EntityManager.TryGetComponent(nodeEntFrom, out XenoArtifactNodeComponent? _))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-commands-failed-to-find-node", ("uid", args[1])));
            return;
        }

        if (!NetEntity.TryParse(args[2], out var nodeNetEntTo)
            || !EntityManager.TryGetEntity(nodeNetEntTo, out var nodeEntTo)
            || !EntityManager.TryGetComponent(nodeEntTo, out XenoArtifactNodeComponent? _))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-commands-failed-to-find-node", ("uid", args[2])));
            return;
        }

        _artifact.AddEdge((artifactEnt.Value, null), nodeEntFrom.Value, nodeEntTo.Value);

        _artifact.RebuildXenoArtifactMetaData((artifactEnt.Value, null));
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

                return CompletionResult.FromHintOptions(completionOptions, Loc.GetString("cmd-xenoartifact-commands-artifact-hint"));
            }
            case 2:
            {
                var query = EntityManager.EntityQueryEnumerator<XenoArtifactNodeComponent>();
                var completionOptions = new List<CompletionOption>();
                if (!NetEntity.TryParse(args[0], out var artifactNetEntId)
                    || !EntityManager.TryGetEntity(artifactNetEntId, out var artifactEntId))
                {
                    return CompletionResult.Empty;
                }

                while (query.MoveNext(out var uid, out var nodeComp))
                {
                    if (nodeComp.Attached == artifactEntId)
                    {
                        var netEntity = EntityManager.GetNetEntity(uid);
                        var hint = nodeComp.TriggerTip == null
                            ? null
                            :Loc.GetString(nodeComp.TriggerTip);
                        completionOptions.Add(new CompletionOption(netEntity.Id.ToString(), hint));
                    }
                }

                return CompletionResult.FromHintOptions(completionOptions, Loc.GetString("cmd-xenoartifact-commands-node-hint"));
            }
            case 3:
            {
                var query = EntityManager.EntityQueryEnumerator<XenoArtifactNodeComponent>();
                var completionOptions = new List<CompletionOption>();
                if (!NetEntity.TryParse(args[0], out var artifactNetEntId)
                    || !EntityManager.TryGetEntity(artifactNetEntId, out var artifactEntId))
                {
                    return CompletionResult.Empty;
                }

                if (!NetEntity.TryParse(args[1], out var fromNodeNetEntId)
                    || !EntityManager.TryGetEntity(fromNodeNetEntId, out var fromNodeEntId))
                {
                    return CompletionResult.Empty;
                }

                while (query.MoveNext(out var uid, out var nodeComp))
                {
                    if (nodeComp.Attached == artifactEntId && uid != fromNodeEntId)
                    {
                        var netEntity = EntityManager.GetNetEntity(uid);
                        var hint = nodeComp.TriggerTip == null
                            ? null
                            : Loc.GetString(nodeComp.TriggerTip);
                        completionOptions.Add(new CompletionOption(netEntity.Id.ToString(), hint));
                    }
                }
                return CompletionResult.FromHintOptions(completionOptions, Loc.GetString("cmd-xenoartifact-commands-node-hint"));
            }
            default:
                return CompletionResult.Empty;
        }
    }
}
