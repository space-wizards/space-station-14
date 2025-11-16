using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoarchaeology.Artifact;

[AdminCommand(AdminFlags.Debug)]
public sealed class CreateNodeInArtifactCommand : LocalizedEntityCommands
{
    [Dependency] private readonly XenoArtifactSystem _artifact = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    /// <inheritdoc />
    public override string Command => "createnodeinartifact";

    /// <inheritdoc />
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 3 or > 4)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var artifactNetEntity)
            || !EntityManager.TryGetEntity(artifactNetEntity, out var target)
            || !EntityManager.TryGetComponent(target.Value, out XenoArtifactComponent? artifactComp))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-commands-failed-to-find-artifact", ("uid", args[0])));
            return;
        }

        if (!_prototypeManager.TryIndex(args[1], out var effectProtoId))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-commands-failed-to-find-effect", ("entProtoId", args[1])));
            return;
        }

        if (!_prototypeManager.TryIndex<XenoArchTriggerPrototype>(args[2], out var trigger))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-commands-failed-to-find-trigger", ("protoId", args[2])));
            return;
        }

        EntityUid? predecessorNodeUid = null;
        var depth = 0;
        if (args.Length == 4
            && NetEntity.TryParse(args[3], out var nodeNetEnt)
            && EntityManager.TryGetEntity(nodeNetEnt, out var nodeEnt)
            && EntityManager.TryGetComponent(nodeEnt, out XenoArtifactNodeComponent? nodeComponent))
        {
            predecessorNodeUid = nodeEnt;
            depth = nodeComponent.Depth + 1;
        }

        var createdNode = _artifact.CreateNode((target.Value, artifactComp), effectProtoId, trigger, depth);
        if (predecessorNodeUid.HasValue)
        {
            _artifact.AddEdge((target.Value, artifactComp), predecessorNodeUid.Value, createdNode);
        }

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

                return CompletionResult.FromHintOptions(completionOptions, Loc.GetString("cmd-xenoartifact-commands-artifact-hint"));
            }
            case 2:
            {
                var query = _prototypeManager.EnumeratePrototypes<EntityPrototype>();
                var completionOptions = new List<CompletionOption>();
                foreach (var entityPrototype in query)
                {
                    if (!entityPrototype.Abstract
                        && entityPrototype.Parents?.Contains(SpawnArtifactWithNodeCommand.ArtifactEffectBaseProtoId.Id) == true)
                    {
                        completionOptions.Add(new CompletionOption(entityPrototype.ID, entityPrototype.Description));
                    }
                }

                return CompletionResult.FromHintOptions(completionOptions, Loc.GetString("cmd-xenoartifact-commands-effect-type-hint"));
            }
            case 3:
            {
                var options = CompletionHelper.PrototypeIDs<XenoArchTriggerPrototype>();
                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-xenoartifact-commands-trigger-type-hint"));
            }
            case 4:
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
                        completionOptions.Add(new CompletionOption(netEntity.Id.ToString(), nodeComp.TriggerTip));
                    }
                }

                return CompletionResult.FromHintOptions(completionOptions, Loc.GetString("cmd-xenoartifact-commands-node-hint"));
            }
            default:
                return CompletionResult.Empty;
        }
    }
}
