using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Console;

namespace Content.Server.Xenoarchaeology.Artifact;

/// <summary> Command for unlocking a specific node of a xeno artifact. </summary>
[AdminCommand(AdminFlags.Debug)]
public sealed class UnlockNodeCommand : LocalizedEntityCommands
{
    [Dependency] private readonly XenoArtifactSystem _artiSystem = default!;

    public override string Command => "unlocknode";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[1], out var netNode) || !EntityManager.TryGetEntity(netNode, out var entityUid))
        {
            shell.WriteError(Loc.GetString("shell-could-not-find-entity-with-uid", ("uid", args[1])));
            return;
        }

        _artiSystem.SetNodeUnlocked(entityUid.Value);
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
                    completionOptions.Add(new CompletionOption(uid.ToString()));
                }

                return CompletionResult.FromHintOptions(completionOptions, Loc.GetString("cmd-unlocknode-artifact-hint"));
            }
            case 2 when
                NetEntity.TryParse(args[0], out var netEnt) &&
                EntityManager.TryGetEntity(netEnt, out var artifactUid) &&
                EntityManager.TryGetComponent<XenoArtifactComponent>(artifactUid, out var comp):
            {
                var result = new List<CompletionOption>();
                foreach (var node in _artiSystem.GetAllNodes((artifactUid.Value, comp)))
                {
                    var metaData = EntityManager.MetaQuery.Comp(artifactUid.Value);
                    var entityUidStr = EntityManager.GetNetEntity(node).ToString();
                    var completionOption = new CompletionOption(entityUidStr, metaData.EntityName);
                    result.Add(completionOption);
                }

                return CompletionResult.FromHintOptions(result, Loc.GetString("cmd-unlocknode-node-hint"));
            }
            default:
                return CompletionResult.Empty;
        }
    }
}
