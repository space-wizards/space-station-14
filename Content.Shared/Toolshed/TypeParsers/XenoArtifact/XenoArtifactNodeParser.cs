using System.Linq;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Shared.Toolshed.TypeParsers.XenoArtifact;

/// <summary>
/// Argument parser for toolshed commands, which should autocomplete artifact nodes that exists on artifact.
/// </summary>
public sealed partial class XenoArtifactNodeParser : CustomCompletionParser<(Entity<XenoArtifactComponent>, Entity<XenoArtifactNodeComponent>)>
{
    [Dependency] private IEntityManager _entityManager = default!;

    /// <inheritdoc />
    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        if (!Toolshed.TryParse<Entity<XenoArtifactComponent>>(ctx, out var artifactEnt))
        {
            return GetHintedEntities<XenoArtifactComponent>(arg);
        }

        var hint = ToolshedCommand.GetArgHint(arg, typeof(Entity<XenoArtifactNodeComponent>));

        var artifact = _entityManager.System<SharedXenoArtifactSystem>();
        var list = artifact.GetAllNodes(artifactEnt)
            .Select(
                node =>
                {
                    var metadata = _entityManager.GetComponent<MetaDataComponent>(node);
                    var effect = Loc.GetString(metadata.EntityDescription);
                    var trigger = node.Comp.TriggerTip.HasValue
                        ? Loc.GetString(node.Comp.TriggerTip)
                        : "Unknown";
                    return new CompletionOption(
                        node.Owner.ToString(),
                        Loc.GetString(
                            "command-xenoartifact-common-node-hint",
                            ("depth", node.Comp.Depth),
                            ("nodeId", artifact.GetNodeId(node.Owner)),
                            ("trigger", trigger),
                            ("effect", effect)
                        )
                    );
                });

        return CompletionResult.FromHintOptions(list, hint);
    }

    private CompletionResult? GetHintedEntities<T>(CommandArgument? arg) where T : IComponent
    {
        var hint = ToolshedCommand.GetArgHint(arg, typeof(NetEntity));

        // Avoid dumping too many entities
        if (_entityManager.Count<T>() > 128)
            return CompletionResult.FromHint(hint);

        var query = _entityManager.AllEntityQueryEnumerator<T, MetaDataComponent>();
        var list = new List<CompletionOption>();
        while (query.MoveNext(out _, out var metadata))
        {
            list.Add(new CompletionOption(metadata.NetEntity.ToString(), metadata.EntityName));
        }

        return CompletionResult.FromHintOptions(list, hint);
    }
}
