using System.Text;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Toolshed.TypeParsers;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Xenoarchaeology.Artifact.Toolshed;

/// <summary>
/// Toolshed commands for manipulating xeno artifact.
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed partial class XenoArtifactCommand : ToolshedCommand
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private XenoArtifactSystem? _artifact;
    
    public static readonly EntProtoId ArtifactPrototype = "ComplexXenoArtifactItem";

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
    [CommandImplementation("printmatrix")]
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
    [CommandImplementation("totalresearch")]
    public int TotalResearch([PipedArgument] EntityUid artifactEntityUid)
    {
        _artifact ??= Sys<XenoArtifactSystem>();
        var comp = Comp<XenoArtifactComponent>(artifactEntityUid);

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
    [CommandImplementation("averageresearch")]
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
    [CommandImplementation("unlockallnodes")]
    public void UnlockAllNodes([PipedArgument] EntityUid artifactEntityUid)
    {
        _artifact ??= Sys<XenoArtifactSystem>();
        var comp = Comp<XenoArtifactComponent>(artifactEntityUid);

        var nodes = _artifact.GetAllNodes((artifactEntityUid, comp));
        foreach (var node in nodes)
        {
            _artifact.SetNodeUnlocked((node, node.Comp));
        }
    }

    /// <summary>
    /// Create node in artifact (new on depth 0 or attach next to existing one).
    /// </summary>
    [CommandImplementation("createnode")]
    public void CreateNodeNew(
        [CommandArgument] Entity<XenoArtifactComponent> artifact,
        [CommandArgument(typeof(XenoEffectParser))] ProtoId<EntityPrototype> effect,
        [CommandArgument] ProtoId<XenoArchTriggerPrototype> trigger
    )
    {
        CreateNode(artifact, effect, trigger);
    }

    /// <summary> Add a new node to the given artifact. </summary>
    [CommandImplementation("createnodeatdepth")]
    public void CreateNodeAtDepth(
        [CommandArgument(typeof(XenoArtifactNodeParser))] (Entity<XenoArtifactComponent> Artifact, Entity<XenoArtifactNodeComponent> Node) tuple,
        [CommandArgument(typeof(XenoEffectParser))] ProtoId<EntityPrototype> effect,
        [CommandArgument] ProtoId<XenoArchTriggerPrototype> trigger
    )
    {
        CreateNode(tuple.Artifact, effect, trigger, tuple.Node);
    }

    /// <summary> Spawns a new xeno artifact with single node with the given trigger and effect. </summary>
    [CommandImplementation("spawnartwithnode")]
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
        if (!TryComp(entity, out XenoArtifactComponent? artifactComp))
        {
            return;
        }

        CreateNode((entity, artifactComp), effect, trigger);
    }

    /// <summary> Marks a node as unlocked. </summary>
    [CommandImplementation("unlocknode")]
    public void UnlockNode(
        [CommandArgument(typeof(XenoArtifactNodeParser))]
        (Entity<XenoArtifactComponent> Artifact, Entity<XenoArtifactNodeComponent> Node) tuple
    )
    {
        _artifact ??= Sys<XenoArtifactSystem>();
        _artifact.SetNodeUnlocked(tuple.Node.AsNullable());
    }

    /// <summary> Removes a node from a xeno artifact. </summary>
    [CommandImplementation("removenode")]
    public void RemoveNode(
        [CommandArgument(typeof(XenoArtifactNodeParser))]
        (Entity<XenoArtifactComponent> Artifact, Entity<XenoArtifactNodeComponent> Node) tuple
    )
    {
        _artifact ??= Sys<XenoArtifactSystem>();
        _artifact.RemoveNode(tuple.Artifact.AsNullable(), tuple.Node.AsNullable());
    }

    /// <summary> Adds an edge between two nodes of a xeno artifact. </summary>
    [CommandImplementation("addedge")]
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

        _artifact = Sys<XenoArtifactSystem>();
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

        _artifact ??= Sys<XenoArtifactSystem>();
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
}
