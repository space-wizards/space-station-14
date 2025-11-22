using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoarchaeology.Artifact;

/// <summary>
/// Base type for xeno artifact debug helper commands.
/// </summary>
public abstract class XenoArtifactCommandBase : LocalizedEntityCommands
{
    [Dependency] protected readonly XenoArtifactSystem Artifact = default!;

    /// <summary>
    /// Enumerates through nodes of xeno artifact, passed in <see cref="artifactUid"/>.
    /// Excludes node with <see cref="except"/> if it was passed.
    /// Filters results by 'StartsWith' using passed <see cref="uidSubstring"/>.
    /// </summary>
    protected IEnumerable<CompletionOption> GetNodes(string uidSubstring, EntityUid artifactUid, EntityUid? except = null)
    {

        if (!EntityManager.TryGetComponent<XenoArtifactComponent>(artifactUid, out var comp))
        {
            return Array.Empty<CompletionOption>();
        }

        var result = new List<CompletionOption>();
        foreach (var node in Artifact.GetAllNodes((artifactUid, comp)))
        {
            if (except.HasValue && node.Owner == except)
                continue;

            var entityUidStr = node.Owner.ToString();
            if (!entityUidStr.StartsWith(uidSubstring))
            {
                continue;
            }

            var optionDescription = node.Comp.TriggerTip == null
                ? EntityManager.MetaQuery.Comp(node).EntityDescription
                : Loc.GetString(node.Comp.TriggerTip.Value);
            var completionOption = new CompletionOption(entityUidStr, optionDescription);
            result.Add(completionOption);
        }

        return result;
    }

    /// <summary>
    /// Enumerates all xeno artifact effects.
    /// Filters results by 'StartsWith' using passed <see cref="effectSubstring"/>.
    /// </summary>
    protected IEnumerable<CompletionOption> GetEffects(IPrototypeManager prototypeManager, string effectSubstring)
    {
        var query = prototypeManager.EnumeratePrototypes<EntityPrototype>();
        foreach (var entityPrototype in query)
        {
            if (entityPrototype is { Abstract: false, Parents: not null }
                && Array.IndexOf(entityPrototype.Parents,SpawnArtifactWithNodeCommand.ArtifactEffectBaseProtoId.Id) != -1
                && entityPrototype.Name.StartsWith(effectSubstring))
            {
                yield return new CompletionOption(entityPrototype.ID, entityPrototype.Description);
            }
        }
    }
}
