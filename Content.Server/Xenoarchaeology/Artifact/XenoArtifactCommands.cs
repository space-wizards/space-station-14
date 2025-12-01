using System.Linq;
using System.Text;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server.Xenoarchaeology.Artifact;

/// <summary>
/// Toolshed commands for manipulating xeno artifact.
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class XenoArtifactCommand : ToolshedCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = null!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = null!;

    public static readonly EntProtoId ArtifactPrototype = "BaseXenoArtifact";

    /// <summary> List existing artifacts. </summary>
    [CommandImplementation("list")]
    public IEnumerable<EntityUid> List()
    {
        var query = EntityManager.EntityQueryEnumerator<XenoArtifactComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            yield return uid;
        }
    }

    /// <summary>
    /// Output matrix of artifact nodes and how they are connected.
    /// </summary>
    [CommandImplementation("print_matrix")]
    public string PrintMatrix([PipedArgument] EntityUid artifactEntitUid)
    {
        var comp = EntityManager.GetComponent<XenoArtifactComponent>(artifactEntitUid);

        var nodeCount = comp.NodeVertices.Length;

        var sb = new StringBuilder("\n  |");
        for (var i = 0; i < nodeCount; i++)
        {
            sb.Append($" {i:D2}|");
        }

        AddHorizontalFiller(sb);

        for (var i = 0; i < nodeCount; i++)
        {
            sb.Append($"\n{i:D2}|");
            for (var j = 0; j < nodeCount; j++)
            {
                var value = comp.NodeAdjacencyMatrix[i][j]
                    ? "X"
                    : " ";
                sb.Append($" {value} |");
            }
            AddHorizontalFiller(sb);
        }

        return sb.ToString();

        void AddHorizontalFiller(StringBuilder builder)
        {
            builder.AppendLine();
            builder.Append("--+");
            for (var i = 0; i < nodeCount; i++)
            {
                builder.Append($"---+");
            }
        }
    }

    /// <summary> Output total research points artifact contains. </summary>
    [CommandImplementation("total_research")]
    public int TotalResearch([PipedArgument] EntityUid artifactEntityUid)
    {
        var artiSys = EntityManager.System<XenoArtifactSystem>();
        var comp = EntityManager.GetComponent<XenoArtifactComponent>(artifactEntityUid);

        var sum = 0;

        var nodes = artiSys.GetAllNodes((artifactEntityUid, comp));
        foreach (var node in nodes)
        {
            sum += node.Comp.ResearchValue;
        }

        return sum;
    }

    /// <summary>
    /// Spawns a bunch of artifacts and gets average total research points they can yield.
    /// </summary>
    [CommandImplementation("average_research")]
    public float AverageResearch()
    {
        const int n = 100;
        var sum = 0;

        for (var i = 0; i < n; i++)
        {
            var ent = Spawn(ArtifactPrototype, MapCoordinates.Nullspace);
            sum += TotalResearch(ent);
            Del(ent);
        }

        return (float) sum / n;
    }

    /// <summary> Unlocks all nodes of artifact. </summary>
    [CommandImplementation("unlock_all_nodes")]
    public void UnlockAllNodes([PipedArgument] EntityUid artifactEntityUid)
    {
        var artiSys = EntityManager.System<XenoArtifactSystem>();
        var comp = EntityManager.GetComponent<XenoArtifactComponent>(artifactEntityUid);

        var nodes = artiSys.GetAllNodes((artifactEntityUid, comp));
        foreach (var node in nodes)
        {
            artiSys.SetNodeUnlocked((node, node.Comp));
        }
    }

    /// <summary>
    /// Create node in artifact (new on depth 0 or attach next to existing one).
    /// </summary>
    /// <param name="artifact">A</param>
    /// <param name="effect">B</param>
    /// <param name="trigger">C</param>
    [CommandImplementation("create_node")]
    public void CreateNodeNew(
        [CommandArgument] Entity<XenoArtifactComponent> artifact,
        [CommandArgument(typeof(XenoEffectParser))] ProtoId<EntityPrototype> effect,
        [CommandArgument] ProtoId<XenoArchTriggerPrototype> trigger
    )
    {
        CreateNode(artifact, effect, trigger);
    }

    [CommandImplementation("create_node_at_depth")]
    public void CreateNodeAtDepth(
        [CommandArgument] Entity<XenoArtifactComponent> artifact,
        [CommandArgument(typeof(XenoEffectParser))] ProtoId<EntityPrototype> effect,
        [CommandArgument] ProtoId<XenoArchTriggerPrototype> trigger,
        [CommandArgument(typeof(XenoArtifactNodeParser))] Entity<XenoArtifactNodeComponent> node
    )
    {
        CreateNode(artifact, effect, trigger, node);
    }

    [CommandImplementation("spawn_art_with_node")]
    public void SpawnArtifactWithNode(
        [CommandArgument] ICommonSession target,
        [CommandArgument(typeof(XenoArtifactTypeParser))] ProtoId<EntityPrototype> artifactType,
        [CommandArgument(typeof(XenoEffectParser))] ProtoId<EntityPrototype> effect,
        [CommandArgument] ProtoId<XenoArchTriggerPrototype> trigger
    )
    {
        if (target.AttachedEntity == null)
            return;

        var entity = EntityManager.SpawnNextToOrDrop(artifactType, target.AttachedEntity.Value);
        if (!EntityManager.TryGetComponent(entity, out XenoArtifactComponent? artifactComp))
        {
            return;
        }

        CreateNode((entity, artifactComp), effect, trigger);
    }

    [CommandImplementation("unlock_node")]
    public void UnlockNode(
        [CommandArgument] Entity<XenoArtifactComponent> artifact,
        [CommandArgument(typeof(XenoArtifactNodeParser))] Entity<XenoArtifactNodeComponent> node
    )
    {
        var artifactSystem = _entitySystemManager.GetEntitySystem<XenoArtifactSystem>();
        artifactSystem.SetNodeUnlocked((node,node));
    }

    [CommandImplementation("remove_node")]
    public void RemoveNode(
        [CommandArgument] Entity<XenoArtifactComponent> artifact,
        [CommandArgument(typeof(XenoArtifactNodeParser))] Entity<XenoArtifactNodeComponent> node
    )
    {
        var artifactSystem = _entitySystemManager.GetEntitySystem<XenoArtifactSystem>();
        artifactSystem.RemoveNode((artifact, artifact), (node, node));
    }

    [CommandImplementation("add_edge")]
    public void AddEdge(
        [CommandArgument] Entity<XenoArtifactComponent> artifact,
        [CommandArgument(typeof(XenoArtifactNodeParser))] Entity<XenoArtifactNodeComponent> nodeFrom,
        [CommandArgument(typeof(XenoArtifactNodeParser))] Entity<XenoArtifactNodeComponent> nodeTo
    )
    {
        if(nodeFrom.Owner == nodeTo.Owner)
            return;

        var artifactSystem = _entitySystemManager.GetEntitySystem<XenoArtifactSystem>();
        artifactSystem.AddEdge((artifact, artifact), nodeFrom, nodeTo);
    }

    private void CreateNode(
        Entity<XenoArtifactComponent> artifact,
        ProtoId<EntityPrototype> effect,
        ProtoId<XenoArchTriggerPrototype> trigger,
        Entity<XenoArtifactNodeComponent>? node = null
    )
    {
        var depth = 0;
        if (node.HasValue)
        {
            depth = node.Value.Comp.Depth + 1;
        }

        var artifactSystem = _entitySystemManager.GetEntitySystem<XenoArtifactSystem>();
        if (!_prototypeManager.Resolve(trigger, out var triggerPrototype))
            return;

        var createdNode = artifactSystem.CreateNode(artifact, effect.Id, triggerPrototype, depth);
        if (node.HasValue)
        {
            artifactSystem.AddEdge(artifact.AsNullable(), node.Value, createdNode);
        }
        else
        {
            artifactSystem.RebuildXenoArtifactMetaData(artifact.AsNullable());
        }
    }
}

public sealed class XenoArtifactNodeParser : CustomTypeParser<Entity<XenoArtifactNodeComponent>>
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = null!;
    [Dependency] private readonly IEntityManager _entityManager= null!;

    private Entity<XenoArtifactComponent>? GetPreviousArtifact(ParserContext ctx)
    {
        // We first try to find the argument in our previous chain that takes in an Entity<XenoArtifactComponent>
        // If we can't find one this parser is being used incorrectly.
        if (ctx.Bundle.Arguments == null)
        {
            return null;
        }

        Entity<XenoArtifactComponent>? entity = null;
        foreach (var (_, value) in ctx.Bundle.Arguments)
        {
            if (value is not ParsedValueRef<Entity<XenoArtifactComponent>> valueRef)
                continue;

            entity = valueRef.Value;
            break;
        }

        DebugTools.AssertNotNull(entity);
        return entity;
    }

    public override bool TryParse(ParserContext ctx, out Entity<XenoArtifactNodeComponent> result)
    {
        var entity = GetPreviousArtifact(ctx);
        if (!entity.HasValue)
        {
            result = default;
            return false;
        }

        if (!_entitySystemManager.TryGetEntitySystem<XenoArtifactSystem>(out var xenoArtifactSystem))
        {
            DebugTools.Assert("XenoArtifactNodeParser called out of sim!");
            result = default;
            return false;
        }

        var word = ctx.GetWord();
        if (word == null)
        {
            result = default;
            return false;
        }

        var nodes = xenoArtifactSystem.GetAllNodes(entity.Value);
        foreach (var node in nodes)
        {
            if (node.Owner.ToString() != word)
                continue;

            result = node;
            return true;
        }

        result = default;
        return false;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var entity = GetPreviousArtifact(ctx);
        if (!entity.HasValue)
        {
            return CompletionResult.Empty;
        }

        if (!_entitySystemManager.TryGetEntitySystem<XenoArtifactSystem>(out var xenoArtifactSystem))
        {
            DebugTools.Assert("XenoArtifactNodeParser called out of sim!");
            return CompletionResult.Empty;
        }

        var hint = ToolshedCommand.GetArgHint(arg, typeof(Entity<XenoArtifactNodeComponent>));

        var list = xenoArtifactSystem.GetAllNodes(entity.Value)
            .Select(
                node =>
                {
                    var metadata = _entityManager.GetComponent<MetaDataComponent>(node);
                    return new CompletionOption(
                        node.Owner.ToString(),
                        $"depth {node.Comp.Depth} node {xenoArtifactSystem.GetNodeId(node.Owner)} trigger {Loc.GetString(metadata.EntityDescription)}"
                    );
                });

        return CompletionResult.FromHintOptions(list, hint);
    }
}

public sealed class XenoArtifactTypeParser : CustomTypeParser<ProtoId<EntityPrototype>>
{
    private static readonly EntProtoId ArtifactDummyItem = "DummyArtifactItem";
    private static readonly EntProtoId ArtifactDummyStructure = "DummyArtifactStructure";
    private static readonly EntProtoId[] Options = [ArtifactDummyItem, ArtifactDummyStructure];

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override bool TryParse(ParserContext ctx, out ProtoId<EntityPrototype> result)
    {
        var protoId = ctx.GetWord();
        if (protoId == null)
        {
            result = default;
            return false;
        }

        result = protoId;

        if (Array.IndexOf(Options, protoId) == -1)
            return false;

        if (!_prototypeManager.TryIndex<EntityPrototype>(protoId, out var prototype))
            return false;
        
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        return CompletionResult.FromHintOptions(
            [
                new CompletionOption(ArtifactDummyItem, Loc.GetString("cmd-spawnartifactwithnode-spawn-artifact-item-hint")),
                new CompletionOption(ArtifactDummyStructure, Loc.GetString("cmd-spawnartifactwithnode-spawn-artifact-structure-hint")),
            ],
            Loc.GetString("cmd-spawnartifactwithnode-spawn-artifact-type-hint")
        );
    }
}

public sealed class XenoEffectParser : CustomTypeParser<ProtoId<EntityPrototype>>
{
    private static readonly EntProtoId ArtifactEffectBaseProtoId = "BaseXenoArtifactEffect";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override bool TryParse(ParserContext ctx, out ProtoId<EntityPrototype> result)
    {
        var protoId = ctx.GetWord();
        if (protoId == null)
        {
            result = default;
            return false;
        }

        result = protoId;

        if (!_prototypeManager.TryIndex<EntityPrototype>(protoId, out var prototype))
            return false;

        if (prototype is not { Abstract: false, Parents: not null })
            return false;

        if (Array.IndexOf(prototype.Parents, ArtifactEffectBaseProtoId.Id) == -1)
            return false;

        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var list = new List<CompletionOption>();
        var hint = ToolshedCommand.GetArgHint(arg, typeof(ProtoId<EntityPrototype>));

        var query = _prototypeManager.EnumeratePrototypes<EntityPrototype>();
        foreach (var entityPrototype in query)
        {
            if (entityPrototype is { Abstract: false, Parents: not null }
                && Array.IndexOf(entityPrototype.Parents, ArtifactEffectBaseProtoId.Id) != -1 )
            {
                list.Add(new CompletionOption(entityPrototype.ID, entityPrototype.Description));
            }
        }

        return CompletionResult.FromHintOptions(list, hint);
    }
}
