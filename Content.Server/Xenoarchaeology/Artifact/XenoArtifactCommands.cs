using System.Linq;
using System.Text;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Prototypes;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server.Xenoarchaeology.Artifact;

/// <summary>
/// Toolshed commands for manipulating xeno artifact.
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed partial class XenoArtifactCommand : ToolshedCommand
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private XenoArtifactSystem? _artifact;
    
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
    [CommandImplementation("printMatrix")]
    public string PrintMatrix([CommandArgument] Entity<XenoArtifactComponent> artifactEnt)
    {
        var nodeCount = artifactEnt.Comp.NodeVertices.Length;

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
                var value = artifactEnt.Comp.NodeAdjacencyMatrix[i][j]
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
                builder.Append("---+");
            }
        }
    }

    /// <summary> Output total research points artifact contains. </summary>
    [CommandImplementation("totalResearch")]
    public int TotalResearch([PipedArgument] EntityUid artifactEntityUid)
    {
        _artifact ??= EntityManager.System<XenoArtifactSystem>();
        var comp = EntityManager.GetComponent<XenoArtifactComponent>(artifactEntityUid);

        var sum = 0;

        var nodes = _artifact.GetAllNodes((artifactEntityUid, comp));
        foreach (var node in nodes)
        {
            sum += node.Comp.ResearchValue;
        }

        return sum;
    }

    /// <summary>
    /// Spawns a bunch of artifacts and gets average total research points they can yield.
    /// </summary>
    [CommandImplementation("averageResearch")]
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

        return (float)sum / n;
    }

    /// <summary> Unlocks all nodes of artifact. </summary>
    [CommandImplementation("unlockAllNodes")]
    public void UnlockAllNodes([PipedArgument] EntityUid artifactEntityUid)
    {
        _artifact ??= EntityManager.System<XenoArtifactSystem>();
        var comp = EntityManager.GetComponent<XenoArtifactComponent>(artifactEntityUid);

        var nodes = _artifact.GetAllNodes((artifactEntityUid, comp));
        foreach (var node in nodes)
        {
            _artifact.SetNodeUnlocked((node, node.Comp));
        }
    }

    /// <summary>
    /// Create node in artifact (new on depth 0 or attach next to existing one).
    /// </summary>
    [CommandImplementation("createNode")]
    public void CreateNodeNew(
        [CommandArgument] Entity<XenoArtifactComponent> artifact,
        [CommandArgument(typeof(XenoEffectParser))] ProtoId<EntityPrototype> effect,
        [CommandArgument] ProtoId<XenoArchTriggerPrototype> trigger
    )
    {
        CreateNode(artifact, effect, trigger);
    }

    /// <summary> Add a new node to the given artifact. </summary>
    [CommandImplementation("createNodeAtDepth")]
    public void CreateNodeAtDepth(
        [CommandArgument(typeof(XenoArtifactNodeParser))] (Entity<XenoArtifactComponent> Artifact, Entity<XenoArtifactNodeComponent> Node) tuple,
        [CommandArgument(typeof(XenoEffectParser))] ProtoId<EntityPrototype> effect,
        [CommandArgument] ProtoId<XenoArchTriggerPrototype> trigger
    )
    {
        CreateNode(tuple.Artifact, effect, trigger, tuple.Node);
    }

    /// <summary> Spawns a new xeno artifact with single node with the given trigger and effect. </summary>
    [CommandImplementation("spawnArtWithNode")]
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

    /// <summary> Marks a node as unlocked. </summary>
    [CommandImplementation("unlockNode")]
    public void UnlockNode(
        [CommandArgument(typeof(XenoArtifactNodeParser))]
        (Entity<XenoArtifactComponent> Artifact, Entity<XenoArtifactNodeComponent> Node) tuple
    )
    {
        _artifact ??= EntitySystemManager.GetEntitySystem<XenoArtifactSystem>();
        _artifact.SetNodeUnlocked(tuple.Node.AsNullable());
    }

    /// <summary> Removes a node from a xeno artifact. </summary>
    [CommandImplementation("removeNode")]
    public void RemoveNode(
        [CommandArgument(typeof(XenoArtifactNodeParser))]
        (Entity<XenoArtifactComponent> Artifact, Entity<XenoArtifactNodeComponent> Node) tuple
    )
    {
        _artifact ??= EntitySystemManager.GetEntitySystem<XenoArtifactSystem>();
        _artifact.RemoveNode(tuple.Artifact.AsNullable(), tuple.Node.AsNullable());
    }

    /// <summary> Adds an edge between two nodes of a xeno artifact. </summary>
    [CommandImplementation("addEdge")]
    public void AddEdge(
        [CommandArgument(typeof(XenoArtifactNodeParser))]
        (Entity<XenoArtifactComponent> Artifact, Entity<XenoArtifactNodeComponent> Node) from,
        [CommandArgument(typeof(XenoArtifactNodeParser))]
        (Entity<XenoArtifactComponent> Artifact, Entity<XenoArtifactNodeComponent> Node) to
    )
    {
        // no inter-artifact edges or self-connects allowed
        if (from.Artifact.Owner != to.Artifact.Owner || from.Node.Owner == to.Node.Owner)
            return;

        _artifact = EntitySystemManager.GetEntitySystem<XenoArtifactSystem>();
        _artifact.AddEdge(from.Artifact.AsNullable(), from.Node, to.Node);
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

        _artifact ??= EntitySystemManager.GetEntitySystem<XenoArtifactSystem>();
        if (!_prototypeManager.Resolve(trigger, out var triggerPrototype))
            return;

        var createdNode = _artifact.CreateNode(artifact, effect.Id, triggerPrototype, depth);
        if (node.HasValue)
        {
            _artifact.AddEdge(artifact.AsNullable(), node.Value, createdNode);
        }
        else
        {
            _artifact.RebuildXenoArtifactMetaData(artifact.AsNullable());
        }
    }

    /// <summary>
    /// Argument parser for toolshed commands, which should autocomplete artifact nodes that exists on artifact.
    /// </summary>
    public sealed partial class XenoArtifactNodeParser : CustomTypeParser<(Entity<XenoArtifactComponent>, Entity<XenoArtifactNodeComponent>)>
    {
        [Dependency] private IEntityManager _entityManager = default!;
        [Dependency] private ToolshedManager _toolshedManager = default!;
        [Dependency] private IEntitySystemManager _entitySystemManager = default!;

        private XenoArtifactSystem? _artifact;

        /// <inheritdoc />
        public override bool TryParse(ParserContext parser, out (Entity<XenoArtifactComponent>, Entity<XenoArtifactNodeComponent>) result)
        {
            result = default;

            
            if (!_toolshedManager.TryParse<Entity<XenoArtifactComponent>>(parser, out var artifactEnt))
                return false;

            if (!_toolshedManager.TryParse<Entity<XenoArtifactNodeComponent>>(parser, out var nodeEnt))
                return false;

            result = (artifactEnt, nodeEnt);
            return true;
        }

        /// <inheritdoc />
        public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
        {
            if (!_toolshedManager.TryParse<Entity<XenoArtifactComponent>>(ctx, out var artifactEnt))
            {
                return GetHintedEntities<XenoArtifactComponent>(arg);
            }

            var hint = ToolshedCommand.GetArgHint(arg, typeof(Entity<XenoArtifactNodeComponent>));

            _artifact ??= _entitySystemManager.GetEntitySystem<XenoArtifactSystem>();
            var list = _artifact.GetAllNodes(artifactEnt)
                                .Select(
                                    node =>
                                    {
                                        var metadata = _entityManager.GetComponent<MetaDataComponent>(node);
                                        var entDescription = Loc.GetString(metadata.EntityDescription);
                                        return new CompletionOption(
                                            node.Owner.ToString(),
                                            Loc.GetString(
                                                "command-xenoartifact-common-node-hint",
                                                ("depth", node.Comp.Depth),
                                                ("nodeId", _artifact.GetNodeId(node.Owner)),
                                                ("nodeDetail", entDescription)
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
}

/// <summary>
/// Custom type parser for toolshed commands that will enable choosing between hand-held and
/// stationary artifact types.
/// </summary>
public sealed partial class XenoArtifactTypeParser : CustomTypeParser<ProtoId<EntityPrototype>>
{
    private static readonly EntProtoId ArtifactDummyItem = "DummyArtifactItem";
    private static readonly EntProtoId ArtifactDummyStructure = "DummyArtifactStructure";
    private static readonly EntProtoId[] Options = [ArtifactDummyItem, ArtifactDummyStructure];

    [Dependency] private IPrototypeManager _prototypeManager = default!;

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

        if (!_prototypeManager.TryIndex<EntityPrototype>(protoId, out var _))
            return false;
        
        return true;
    }

    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        return CompletionResult.FromHintOptions(
            [
                new CompletionOption(ArtifactDummyItem, Loc.GetString("command-spawnartifactwithnode-spawn-artifact-item-hint")),
                new CompletionOption(ArtifactDummyStructure, Loc.GetString("command-spawnartifactwithnode-spawn-artifact-structure-hint")),
            ],
            Loc.GetString("command-spawnartifactwithnode-spawn-artifact-type-hint")
        );
    }
}

/// <summary>
/// Custom type parser for toolshed commands
/// that lets choose entity prototype of XenoArtifact effect.
/// </summary>
public sealed partial class XenoEffectParser : CustomTypeParser<ProtoId<EntityPrototype>>
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IEntitySystemManager _systemManager = default!;

    private XenoArtifactSystem? _artifact;

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

        if (prototype is not { Abstract: false })
            return false;

        // all effect prototypes are marked as nodes, as nodes are born from those prototypes
        return prototype.HasComponent<XenoArtifactNodeComponent>();
    }

    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var hint = ToolshedCommand.GetArgHint(arg, typeof(ProtoId<EntityPrototype>));

        _artifact ??= _systemManager.GetEntitySystem<XenoArtifactSystem>();

        return CompletionResult.FromHintOptions(_artifact.EffectPrototypeIds, hint);
    }
}
