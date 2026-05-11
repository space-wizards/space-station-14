using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests;

[TestFixture]
[TestOf(typeof(SharedXenoArtifactSystem))]
public sealed class XenoArtifactTest : GameTest
{
    private const string TestArtifact = "TestArtifact";
    private const string TestArtifactNode = "TestArtifactNode";
    private const string TestGenArtifactFlat = "TestGenArtifactFlat";
    private const string TestGenArtifactTall = "TestGenArtifactTall";
    private const string TestGenArtifactFull = "TestGenArtifactFull";

    [SidedDependency(Side.Server)] private SharedXenoArtifactSystem _sArtifactSystem = null!;

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {TestArtifact}
  parent: BaseXenoArtifact
  name: artifact
  components:
  - type: XenoArtifact
    isGenerationRequired: false
    effectsTable: !type:NestedSelector
      tableId: XenoArtifactEffectsDefaultTable

- type: entity
  id: {TestGenArtifactFlat}
  parent: BaseXenoArtifact
  name: artifact
  components:
  - type: XenoArtifact
    isGenerationRequired: true
    nodeCount:
      min: 2
      max: 2
    segmentSize:
      min: 1
      max: 1
    nodesPerSegmentLayer:
      min: 1
      max: 1
    effectsTable: !type:NestedSelector
      tableId: XenoArtifactEffectsDefaultTable

- type: entity
  id: {TestGenArtifactTall}
  parent: BaseXenoArtifact
  name: artifact
  components:
  - type: XenoArtifact
    isGenerationRequired: true
    nodeCount:
      min: 2
      max: 2
    segmentSize:
      min: 2
      max: 2
    nodesPerSegmentLayer:
      min: 1
      max: 1
    effectsTable: !type:NestedSelector
      tableId: XenoArtifactEffectsDefaultTable

- type: entity
  id: {TestGenArtifactFull}
  name: artifact
  components:
  - type: XenoArtifact
    isGenerationRequired: true
    nodeCount:
      min: 6
      max: 6
    segmentSize:
      min: 6
      max: 6
    nodesPerSegmentLayer:
      min: 2
      max: 2
    effectsTable: !type:NestedSelector
      tableId: XenoArtifactEffectsDefaultTable

- type: entity
  id: {TestArtifactNode}
  name: artifact node
  components:
  - type: XenoArtifactNode
    maxDurability: 3
";

    /// <summary>
    /// Checks that adding nodes and edges properly adds them into the adjacency matrix
    /// </summary>
    [Test]
    [Description("Checks that adding nodes and edges properly adds them into the adjacency matrix")]
    [RunOnSide(Side.Server)]
    public async Task XenoArtifactAddNodeTest()
    {
        var artifactUid = SSpawn(TestArtifact);
        var artifactEnt = (artifactUid, comp: SComp<XenoArtifactComponent>(artifactUid));

        // Create 3 nodes
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node1, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node2, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node3, false));

        Assert.That(_sArtifactSystem.GetAllNodeIndices(artifactEnt).Count(), Is.EqualTo(3));

        // Add connection from 1 -> 2 and 2-> 3
        _sArtifactSystem.AddEdge(artifactEnt, node1!.Value, node2!.Value, false);
        _sArtifactSystem.AddEdge(artifactEnt, node2!.Value, node3!.Value, false);

        // Assert that successors and direct successors are counted correctly for node 1.
        Assert.That(_sArtifactSystem.GetDirectSuccessorNodes(artifactEnt, node1!.Value), Has.Count.EqualTo(1));
        Assert.That(_sArtifactSystem.GetSuccessorNodes(artifactEnt, node1!.Value), Has.Count.EqualTo(2));
        // Assert that we didn't somehow get predecessors on node 1.
        Assert.That(_sArtifactSystem.GetDirectPredecessorNodes(artifactEnt, node1!.Value), Is.Empty);
        Assert.That(_sArtifactSystem.GetPredecessorNodes(artifactEnt, node1!.Value), Is.Empty);

        // Assert that successors and direct successors are counted correctly for node 2.
        Assert.That(_sArtifactSystem.GetDirectSuccessorNodes(artifactEnt, node2!.Value), Has.Count.EqualTo(1));
        Assert.That(_sArtifactSystem.GetSuccessorNodes(artifactEnt, node2!.Value), Has.Count.EqualTo(1));
        // Assert that predecessors and direct predecessors are counted correctly for node 2.
        Assert.That(_sArtifactSystem.GetDirectPredecessorNodes(artifactEnt, node2!.Value), Has.Count.EqualTo(1));
        Assert.That(_sArtifactSystem.GetPredecessorNodes(artifactEnt, node2!.Value), Has.Count.EqualTo(1));

        // Assert that successors and direct successors are counted correctly for node 3.
        Assert.That(_sArtifactSystem.GetDirectSuccessorNodes(artifactEnt, node3!.Value), Is.Empty);
        Assert.That(_sArtifactSystem.GetSuccessorNodes(artifactEnt, node3!.Value), Is.Empty);
        // Assert that predecessors and direct predecessors are counted correctly for node 3.
        Assert.That(_sArtifactSystem.GetDirectPredecessorNodes(artifactEnt, node3!.Value), Has.Count.EqualTo(1));
        Assert.That(_sArtifactSystem.GetPredecessorNodes(artifactEnt, node3!.Value), Has.Count.EqualTo(2));
    }

    /// <summary>
    /// Checks to make sure that removing nodes properly cleans up all connections.
    /// </summary>
    [Test]
    [Description("Checks to make sure that removing nodes properly cleans up all connections.")]
    [RunOnSide(Side.Server)]
    public async Task XenoArtifactRemoveNodeTest()
    {
        var artifactUid = SSpawn(TestArtifact);
        var artifactEnt = (artifactUid, comp: SComp<XenoArtifactComponent>(artifactUid));

        // Create 3 nodes
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node1, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node2, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node3, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node4, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node5, false));

        Assert.That(_sArtifactSystem.GetAllNodeIndices(artifactEnt).Count(), Is.EqualTo(5));

        // Add connection: 1 -> 2 -> 3 -> 4 -> 5
        _sArtifactSystem.AddEdge(artifactEnt, node1!.Value, node2!.Value, false);
        _sArtifactSystem.AddEdge(artifactEnt, node2!.Value, node3!.Value, false);
        _sArtifactSystem.AddEdge(artifactEnt, node3!.Value, node4!.Value, false);
        _sArtifactSystem.AddEdge(artifactEnt, node4!.Value, node5!.Value, false);

        // Make sure we have a continuous connection between the two ends of the graph.
        Assert.That(_sArtifactSystem.GetSuccessorNodes(artifactEnt, node1.Value), Has.Count.EqualTo(4));
        Assert.That(_sArtifactSystem.GetPredecessorNodes(artifactEnt, node5.Value), Has.Count.EqualTo(4));

        // Remove the node and make sure it's no longer in the artifact.
        Assert.That(_sArtifactSystem.RemoveNode(artifactEnt, node3!.Value, false));
        Assert.That(_sArtifactSystem.TryGetIndex(artifactEnt, node3!.Value, out _), Is.False, "Node 3 still present in artifact.");

        // Check to make sure that we got rid of all the connections.
        Assert.That(_sArtifactSystem.GetSuccessorNodes(artifactEnt, node2!.Value), Is.Empty);
        Assert.That(_sArtifactSystem.GetPredecessorNodes(artifactEnt, node4!.Value), Is.Empty);
    }

    /// <summary>
    /// Sets up series of linked nodes and ensures that resizing the adjacency matrix doesn't disturb the connections
    /// </summary>
    [Test]
    [Description("Sets up series of linked nodes and ensures that resizing the adjacency matrix doesn't disturb the connections")]
    [RunOnSide(Side.Server)]
    public async Task XenoArtifactResizeTest()
    {
        var artifactUid = SSpawn(TestArtifact);
        var artifactEnt = (artifactUid, comp: SComp<XenoArtifactComponent>(artifactUid));

        // Create 3 nodes
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node1, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node2, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node3, false));

        // Add connection: 1 -> 2 -> 3
        _sArtifactSystem.AddEdge(artifactEnt, node1!.Value, node2!.Value, false);
        _sArtifactSystem.AddEdge(artifactEnt, node2!.Value, node3!.Value, false);

        // Make sure our connection is set up
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node1.Value, node2.Value));
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node2.Value, node3.Value));
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node2.Value, node1.Value), Is.False);
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node3.Value, node2.Value), Is.False);
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node1.Value, node3.Value), Is.False);
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node3.Value, node1.Value), Is.False);

        Assert.That(_sArtifactSystem.GetIndex(artifactEnt, node1!.Value), Is.Zero);
        Assert.That(_sArtifactSystem.GetIndex(artifactEnt, node2!.Value), Is.EqualTo(1));
        Assert.That(_sArtifactSystem.GetIndex(artifactEnt, node3!.Value), Is.EqualTo(2));

        // Add a new node, resizing the original adjacency matrix and array.
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node4));

        // Check that our connections haven't changed.
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node1.Value, node2.Value));
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node2.Value, node3.Value));
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node2.Value, node1.Value), Is.False);
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node3.Value, node2.Value), Is.False);
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node1.Value, node3.Value), Is.False);
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node3.Value, node1.Value), Is.False);

        // Has our array shifted any when we resized?
        Assert.That(_sArtifactSystem.GetIndex(artifactEnt, node1!.Value), Is.Zero);
        Assert.That(_sArtifactSystem.GetIndex(artifactEnt, node2!.Value), Is.EqualTo(1));
        Assert.That(_sArtifactSystem.GetIndex(artifactEnt, node3!.Value), Is.EqualTo(2));

        // Check that 4 didn't somehow end up with connections
        Assert.That(_sArtifactSystem.GetPredecessorNodes(artifactEnt, node4!.Value), Is.Empty);
        Assert.That(_sArtifactSystem.GetSuccessorNodes(artifactEnt, node4!.Value), Is.Empty);
    }

    /// <summary>
    /// Checks if removing a node and adding a new node into its place in the adjacency matrix doesn't accidentally retain extra data.
    /// </summary>
    [Test]
    [Description("Checks if removing a node and adding a new node into its place in the adjacency matrix doesn't accidentally retain extra data.")]
    [RunOnSide(Side.Server)]
    public async Task XenoArtifactReplaceTest()
    {
        var artifactUid = SSpawn(TestArtifact);
        var artifactEnt = (artifactUid, Comp: SComp<XenoArtifactComponent>(artifactUid));

        // Create 3 nodes
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node1, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node2, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node3, false));

        // Add connection: 1 -> 2 -> 3
        _sArtifactSystem.AddEdge(artifactEnt, node1!.Value, node2!.Value, false);
        _sArtifactSystem.AddEdge(artifactEnt, node2!.Value, node3!.Value, false);

        // Make sure our connection is set up
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node1.Value, node2.Value));
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node2.Value, node3.Value));

        // Remove middle node, severing connections
        _sArtifactSystem.RemoveNode(artifactEnt, node2!.Value, false);

        // Make sure our connection are properly severed.
        Assert.That(_sArtifactSystem.GetSuccessorNodes(artifactEnt, node1.Value), Is.Empty);
        Assert.That(_sArtifactSystem.GetPredecessorNodes(artifactEnt, node3.Value), Is.Empty);

        // Make sure our matrix is 3x3
        Assert.That(artifactEnt.Comp.NodeAdjacencyMatrixRows, Is.EqualTo(3));
        Assert.That(artifactEnt.Comp.NodeAdjacencyMatrixColumns, Is.EqualTo(3));

        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node4, false));

        // Make sure that adding in a new node didn't add a new slot but instead re-used the middle slot.
        Assert.That(artifactEnt.Comp.NodeAdjacencyMatrixRows, Is.EqualTo(3));
        Assert.That(artifactEnt.Comp.NodeAdjacencyMatrixColumns, Is.EqualTo(3));

        // Ensure that all connections are still severed
        Assert.That(_sArtifactSystem.GetSuccessorNodes(artifactEnt, node1.Value), Is.Empty);
        Assert.That(_sArtifactSystem.GetPredecessorNodes(artifactEnt, node3.Value), Is.Empty);
        Assert.That(_sArtifactSystem.GetSuccessorNodes(artifactEnt, node4!.Value), Is.Empty);
        Assert.That(_sArtifactSystem.GetPredecessorNodes(artifactEnt, node4!.Value), Is.Empty);
    }

    /// <summary>
    /// Checks if the active nodes are properly detected.
    /// </summary>
    [Test]
    [Description("Checks if the active nodes are properly detected.")]
    [RunOnSide(Side.Server)]
    public async Task XenoArtifactBuildActiveNodesTest()
    {
        var artifactUid = SSpawn(TestArtifact);
        Entity<XenoArtifactComponent> artifactEnt = (artifactUid, SComp<XenoArtifactComponent>(artifactUid));

        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node1, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node2, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node3, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node4, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node5, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node6, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node7, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node8, false));

        //                       /----( 6 )
        //           /----[*3 ]-/----( 7 )----( 8 )
        //          /
        //         /           /----[*5 ]
        // [ 1 ]--/----[ 2 ]--/----( 4 )
        // Diagram of the example generation. Nodes in [brackets] are unlocked, nodes in (braces) are locked
        // and nodes with an *asterisk are supposed to be active.
        _sArtifactSystem.AddEdge(artifactEnt, node1!.Value, node2!.Value, false);
        _sArtifactSystem.AddEdge(artifactEnt, node1!.Value, node3!.Value, false);

        _sArtifactSystem.AddEdge(artifactEnt, node2!.Value, node4!.Value, false);
        _sArtifactSystem.AddEdge(artifactEnt, node2!.Value, node5!.Value, false);

        _sArtifactSystem.AddEdge(artifactEnt, node3!.Value, node6!.Value, false);
        _sArtifactSystem.AddEdge(artifactEnt, node3!.Value, node7!.Value, false);

        _sArtifactSystem.AddEdge(artifactEnt, node7!.Value, node8!.Value, false);

        _sArtifactSystem.SetNodeUnlocked(node1!.Value);
        _sArtifactSystem.SetNodeUnlocked(node2!.Value);
        _sArtifactSystem.SetNodeUnlocked(node3!.Value);
        _sArtifactSystem.SetNodeUnlocked(node5!.Value);

        NetEntity[] expectedActiveNodes =
        [
            SEntMan.GetNetEntity(node3!.Value.Owner),
            SEntMan.GetNetEntity(node5!.Value.Owner)
        ];
        Assert.That(artifactEnt.Comp.CachedActiveNodes, Is.SupersetOf(expectedActiveNodes));
        Assert.That(artifactEnt.Comp.CachedActiveNodes, Has.Count.EqualTo(expectedActiveNodes.Length));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public async Task XenoArtifactGenerateSegmentsTest()
    {
        var artifact1Uid = SSpawn(TestGenArtifactFlat);
        Entity<XenoArtifactComponent> artifact1Ent = (artifact1Uid, SComp<XenoArtifactComponent>(artifact1Uid));

        var segments1 = _sArtifactSystem.GetSegments(artifact1Ent);
        Assert.That(segments1, Has.Count.EqualTo(2));
        Assert.That(segments1[0], Has.Count.EqualTo(1));
        Assert.That(segments1[1], Has.Count.EqualTo(1));

        var artifact2Uid = SSpawn(TestGenArtifactTall);
        Entity<XenoArtifactComponent> artifact2Ent = (artifact2Uid, SComp<XenoArtifactComponent>(artifact2Uid));

        var segments2 = _sArtifactSystem.GetSegments(artifact2Ent);
        Assert.That(segments2, Has.Count.EqualTo(1));
        Assert.That(segments2[0], Has.Count.EqualTo(2));

        var artifact3Uid = SSpawn(TestGenArtifactFull);
        Entity<XenoArtifactComponent> artifact3Ent = (artifact3Uid, SComp<XenoArtifactComponent>(artifact3Uid));

        var segments3 = _sArtifactSystem.GetSegments(artifact3Ent);
        Assert.That(segments3, Has.Count.EqualTo(1));
        Assert.That(segments3.Sum(x => x.Count), Is.EqualTo(6));
        var nodesDepths = segments3[0].Select(x => x.Comp.Depth).ToArray();
        Assert.That(nodesDepths.Distinct().Count(), Is.EqualTo(3));
        var grouped = nodesDepths.ToLookup(x => x);
        Assert.That(grouped[0].Count(), Is.EqualTo(2));
        Assert.That(grouped[1].Count(), Is.GreaterThanOrEqualTo(2)); // tree is attempting sometimes to get wider (so it will look like a tree)
        Assert.That(grouped[2].Count(), Is.LessThanOrEqualTo(2)); // maintain same width or, if we used 3 nodes on previous layer - we only have 1 left!
    }
}
