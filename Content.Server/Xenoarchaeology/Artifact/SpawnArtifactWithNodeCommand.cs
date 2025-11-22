using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoarchaeology.Artifact;

[AdminCommand(AdminFlags.Debug)]
public sealed class SpawnArtifactWithNodeCommand : XenoArtifactCommandBase
{
    public static readonly EntProtoId ArtifactEffectBaseProtoId = "BaseXenoArtifactEffect";

    private static readonly EntProtoId ArtifactDummyItem = "DummyArtifactItem";
    private static readonly EntProtoId ArtifactDummyStructure = "DummyArtifactStructure";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    /// <inheritdoc />
    public override string Command => "spawnartifactwithnode";

    /// <inheritdoc />
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var target = shell.Player?.AttachedEntity;
        if (!target.HasValue)
        {
            shell.WriteLine(Loc.GetString("cmd-spawnartifactwithnode-failed-to-find-current-player"));
            return;
        }

        if (!_prototypeManager.Resolve(args[1], out var effect))
        {
            shell.WriteError(Loc.GetString("cmd-xenoartifact-failed-to-find-effect", ("uid", args[1])));
            return;
        }

        if (!_prototypeManager.Resolve(args[2], out XenoArchTriggerPrototype? trigger))
        {
            shell.WriteError(Loc.GetString("cmd-xenoartifact-failed-to-find-trigger", ("protoId", args[2])));
            return;
        }

        var entity = EntityManager.SpawnNextToOrDrop(args[0], target.Value);
        if (!EntityManager.TryGetComponent(entity, out XenoArtifactComponent? artifactComp))
        {
            shell.WriteError(Loc.GetString("cmd-spawnartifactwithnode-invalid-prototype-id"));
            return;
        }

        Artifact.CreateNode((entity, artifactComp), effect, trigger);
        Artifact.RebuildXenoArtifactMetaData((entity, artifactComp));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                    [
                        new CompletionOption(ArtifactDummyItem, Loc.GetString("cmd-spawnartifactwithnode-spawn-artifact-item-hint")),
                        new CompletionOption(ArtifactDummyStructure, Loc.GetString("cmd-spawnartifactwithnode-spawn-artifact-structure-hint")),
                    ],
                    Loc.GetString("cmd-spawnartifactwithnode-spawn-artifact-type-hint")
                ),
            2 => CompletionResult.FromHintOptions(
                GetEffects(_prototypeManager, args[1]),
                Loc.GetString("cmd-xenoartifact-common-effect-type-hint")
            ),
            3 => CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<XenoArchTriggerPrototype>(proto: _prototypeManager),
                Loc.GetString("cmd-xenoartifact-common-trigger-type-hint")
            ),
            _ => CompletionResult.Empty
        };
    }
}
