using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoarchaeology.Artifact;

[AdminCommand(AdminFlags.Debug)]
public sealed class CreateNodeInArtifactCommand : XenoArtifactCommandBase
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

        if (!EntityUid.TryParse(args[0], out var target)
            || !EntityManager.TryGetComponent(target, out XenoArtifactComponent? artifactComp))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-common-failed-to-find-artifact", ("uid", args[0])));
            return;
        }

        if (!_prototypeManager.TryIndex(args[1], out var effectProtoId))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-common-failed-to-find-effect", ("entProtoId", args[1])));
            return;
        }

        if (!_prototypeManager.TryIndex<XenoArchTriggerPrototype>(args[2], out var trigger))
        {
            shell.WriteLine(Loc.GetString("cmd-xenoartifact-common-failed-to-find-trigger", ("protoId", args[2])));
            return;
        }

        EntityUid? predecessorNodeUid = null;
        var depth = 0;
        if (args.Length == 4
            && EntityUid.TryParse(args[3], out var nodeEnt)
            && EntityManager.TryGetComponent(nodeEnt, out XenoArtifactNodeComponent? nodeComponent))
        {
            predecessorNodeUid = nodeEnt;
            depth = nodeComponent.Depth + 1;
        }

        var createdNode = _artifact.CreateNode((target, artifactComp), effectProtoId, trigger, depth);
        if (predecessorNodeUid.HasValue)
        {
            _artifact.AddEdge((target, artifactComp), predecessorNodeUid.Value, createdNode);
        }
        else
        {
            _artifact.RebuildXenoArtifactMetaData((target, artifactComp));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                    ContentCompletionHelper.ByComponentAndEntityUid<XenoArtifactComponent>(args[0], EntityManager),
                    Loc.GetString("cmd-xenoartifact-common-artifact-hint")
                ),
            2 => CompletionResult.FromHintOptions(
                    GetEffects(_prototypeManager, args[1]),
                    Loc.GetString("cmd-xenoartifact-common-effect-type-hint")
                ),
            3 => CompletionResult.FromHintOptions(
                    CompletionHelper.PrototypeIDs<XenoArchTriggerPrototype>(proto: _prototypeManager),
                    Loc.GetString("cmd-xenoartifact-common-trigger-type-hint")
                ),
            4 when EntityUid.TryParse(args[0], out var artifactUid)
                => CompletionResult.FromHintOptions(
                    GetNodes(args[3], artifactUid),
                    Loc.GetString("cmd-xenoartifact-common-node-hint")
                ),
            _ => CompletionResult.Empty
        };
    }
}
