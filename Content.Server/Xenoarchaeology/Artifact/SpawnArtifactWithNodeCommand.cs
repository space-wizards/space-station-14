using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoarchaeology.Artifact;

[AdminCommand(AdminFlags.Debug)]
public sealed class SpawnArtifactWithNodeCommand : LocalizedEntityCommands
{
    private static readonly EntProtoId ArtifactDummyItem = "DummyArtifactItem";
    private static readonly EntProtoId ArtifactDummyStructure = "DummyArtifactStructure";
    public static readonly EntProtoId ArtifactEffectBaseProtoId = "BaseXenoArtifactEffect";

    [Dependency] private readonly XenoArtifactSystem _artifact = default!;
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

        _artifact.CreateNode((entity, artifactComp), effect, trigger);
        _artifact.RebuildXenoArtifactMetaData((entity, artifactComp));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
            {
                CompletionOption[] completionOptions =
                [
                    new CompletionOption(ArtifactDummyItem, Loc.GetString("cmd-spawnartifactwithnode-spawn-artifact-item-hint")),
                    new CompletionOption(ArtifactDummyStructure, Loc.GetString("cmd-spawnartifactwithnode-spawn-artifact-structure-hint")),
                ];
                return CompletionResult.FromHintOptions(completionOptions, Loc.GetString("cmd-spawnartifactwithnode-spawn-artifact-type-hint"));
            }
            case 2:
            {
                var query = _prototypeManager.EnumeratePrototypes<EntityPrototype>(); // needs cache?
                var completionOptions = new List<CompletionOption>();
                foreach (var entityPrototype in query)
                {
                    if (!entityPrototype.Abstract
                        && entityPrototype.Parents?.Contains(ArtifactEffectBaseProtoId.Id) == true)
                    {
                        completionOptions.Add(new CompletionOption(entityPrototype.ID, entityPrototype.Description));
                    }
                }

                return CompletionResult.FromHintOptions(completionOptions, Loc.GetString("cmd-xenoartifact-commands-effect-type-hint"));
            }
            case 3:
            {
                var query = _prototypeManager.EnumeratePrototypes<XenoArchTriggerPrototype>();
                var completionOptions = new List<CompletionOption>();
                foreach (var prototype in query)
                {
                    completionOptions.Add(new CompletionOption(prototype.ID, Loc.GetString(prototype.Tip)));
                }

                return CompletionResult.FromHintOptions(completionOptions, Loc.GetString("cmd-xenoartifact-commands-trigger-type-hint"));
            }
            default:
                return CompletionResult.Empty;
        }
    }
}
