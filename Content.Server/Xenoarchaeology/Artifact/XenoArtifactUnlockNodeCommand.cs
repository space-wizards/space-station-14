using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Console;

namespace Content.Server.Xenoarchaeology.Artifact;

/// <summary> Command for unlocking specific node of xeno artifact. </summary>
[AdminCommand(AdminFlags.Debug)]
public sealed class XenoArtifactUnlockNodeCommand : LocalizedCommands
{
    [Dependency] private readonly EntityManager _entities = default!;

    /// <inheritdoc />
    public override string Command => "unlocknode";

    /// <inheritdoc />
    public override string Description => Loc.GetString("cmd-unlocknode-desc");

    /// <inheritdoc />
    public override string Help => Loc.GetString("cmd-unlocknode-help");

    /// <inheritdoc />
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-unlocknode-arg-num"));
            return;
        }

        if (!NetEntity.TryParse(args[1], out var netNode))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-unlocknode-invalid-entity"));
            return;
        }

        if (!_entities.TryGetEntity(netNode, out var entityUid))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-unlocknode-invalid-entity"));
            return;
        }
        _entities.System<XenoArtifactSystem>()
                 .SetNodeUnlocked(entityUid.Value);
    }

    /// <inheritdoc />
    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var query = _entities.EntityQueryEnumerator<XenoArtifactComponent>();
            var completionOptions = new List<CompletionOption>();
            while (query.MoveNext(out var uid, out _))
            {
                completionOptions.Add(new CompletionOption(uid.ToString()));
            }

            return CompletionResult.FromHintOptions(completionOptions, "<artifact uid>");
        }

        if (args.Length == 2 &&
            NetEntity.TryParse(args[0], out var netEnt) &&
            _entities.TryGetEntity(netEnt, out var artifactUid) &&
            _entities.TryGetComponent<XenoArtifactComponent>(artifactUid, out var comp))
        {
            var artifactSystem = _entities.System<XenoArtifactSystem>();

            var result = new List<CompletionOption>();
            foreach (var node in artifactSystem.GetAllNodes((artifactUid.Value, comp)))
            {
                var metaData = _entities.MetaQuery.Comp(artifactUid.Value);
                var entityUidStr = _entities.GetNetEntity(node)
                                            .ToString();
                var completionOption = new CompletionOption(entityUidStr, metaData.EntityName);
                result.Add(completionOption);
            }

            return CompletionResult.FromHintOptions(result, "<node uid>");
        }

        return CompletionResult.Empty;
    }
}
