#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests;

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
    [RunOnSide(Side.Server)]
    [Description("Checks that adding nodes and edges properly adds them into the adjacency matrix")]
    public async Task XenoArtifactAddNodeTest()
    {
        var artifactUid = SSpawn(TestArtifact);
        var artifactEnt = SEntity<XenoArtifactComponent>(artifactUid).AsNullable();

        // Create 3 nodes
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node1, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node2, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node3, false));

        Assert.That(_sArtifactSystem.GetAllNodeIndices(artifactEnt!).Count(), Is.EqualTo(3));

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
    [RunOnSide(Side.Server)]
    [Description("Checks to make sure that removing nodes properly cleans up all connections.")]
    public async Task XenoArtifactRemoveNodeTest()
    {
        var artifactUid = SSpawn(TestArtifact);
        var artifactEnt = SEntity<XenoArtifactComponent>(artifactUid).AsNullable();

        // Create 3 nodes
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node1, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node2, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node3, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node4, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node5, false));

        Assert.That(_sArtifactSystem.GetAllNodeIndices(artifactEnt!).Count(), Is.EqualTo(5));

        // Add connection: 1 -> 2 -> 3 -> 4 -> 5
        _sArtifactSystem.AddEdge(artifactEnt, node1!.Value, node2!.Value, false);
        _sArtifactSystem.AddEdge(artifactEnt, node2!.Value, node3!.Value, false);
        _sArtifactSystem.AddEdge(artifactEnt, node3!.Value, node4!.Value, false);
        _sArtifactSystem.AddEdge(artifactEnt, node4!.Value, node5!.Value, false);

        // Make sure we have a continuous connection between the two ends of the graph.
        Assert.That(_sArtifactSystem.GetSuccessorNodes(artifactEnt, node1.Value), Has.Count.EqualTo(4));
        Assert.That(_sArtifactSystem.GetPredecessorNodes(artifactEnt, node5.Value), Has.Count.EqualTo(4));

        // Remove the node and make sure it's no longer in the artifact.
        Assert.That(_sArtifactSystem.RemoveNode(artifactEnt, node3!.Value.AsNullable(), false));
        Assert.That(_sArtifactSystem.TryGetIndex(artifactEnt, node3!.Value, out _), Is.False, "Node 3 still present in artifact.");

        // Check to make sure that we got rid of all the connections.
        Assert.That(_sArtifactSystem.GetSuccessorNodes(artifactEnt, node2!.Value), Is.Empty);
        Assert.That(_sArtifactSystem.GetPredecessorNodes(artifactEnt, node4!.Value), Is.Empty);
    }

    /// <summary>
    /// Sets up series of linked nodes and ensures that resizing the adjacency matrix doesn't disturb the connections
    /// </summary>
    [Test]
    [RunOnSide(Side.Server)]
    [Description("Sets up series of linked nodes and ensures that resizing the adjacency matrix doesn't disturb the connections")]
    public async Task XenoArtifactResizeTest()
    {
        var artifactUid = SSpawn(TestArtifact);
        var artifactEnt = SEntity<XenoArtifactComponent>(artifactUid).AsNullable();

        // Create 3 nodes
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node1, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node2, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node3, false));

        // Add connection: 1 -> 2 -> 3
        _sArtifactSystem.AddEdge(artifactEnt, node1!.Value, node2!.Value, false);
        _sArtifactSystem.AddEdge(artifactEnt, node2!.Value, node3!.Value, false);

        var node1Null = node1.Value.AsNullable();
        var node2Null = node2.Value.AsNullable();
        var node3Null = node3.Value.AsNullable();

        // Make sure our connection is set up
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node1Null, node2Null));
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node2Null, node3Null));
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node2Null, node1Null), Is.False);
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node3Null, node2Null), Is.False);
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node1Null, node3Null), Is.False);
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node3Null, node1Null), Is.False);

        Assert.That(_sArtifactSystem.GetIndex(artifactEnt!, node1!.Value), Is.Zero);
        Assert.That(_sArtifactSystem.GetIndex(artifactEnt!, node2!.Value), Is.EqualTo(1));
        Assert.That(_sArtifactSystem.GetIndex(artifactEnt!, node3!.Value), Is.EqualTo(2));

        // Add a new node, resizing the original adjacency matrix and array.
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node4));

        // Check that our connections haven't changed.
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node1Null, node2Null));
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node2Null, node3Null));
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node2Null, node1Null), Is.False);
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node3Null, node2Null), Is.False);
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node1Null, node3Null), Is.False);
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node3Null, node1Null), Is.False);

        // Has our array shifted any when we resized?
        Assert.That(_sArtifactSystem.GetIndex(artifactEnt!, node1!.Value), Is.Zero);
        Assert.That(_sArtifactSystem.GetIndex(artifactEnt!, node2!.Value), Is.EqualTo(1));
        Assert.That(_sArtifactSystem.GetIndex(artifactEnt!, node3!.Value), Is.EqualTo(2));

        // Check that 4 didn't somehow end up with connections
        Assert.That(_sArtifactSystem.GetPredecessorNodes(artifactEnt, node4!.Value), Is.Empty);
        Assert.That(_sArtifactSystem.GetSuccessorNodes(artifactEnt, node4!.Value), Is.Empty);
    }

    /// <summary>
    /// Checks if removing a node and adding a new node into its place in the adjacency matrix doesn't accidentally retain extra data.
    /// </summary>
    [Test]
    [RunOnSide(Side.Server)]
    [Description("Checks if removing a node and adding a new node into its place in the adjacency matrix doesn't accidentally retain extra data.")]
    public async Task XenoArtifactReplaceTest()
    {
        var artifactUid = SSpawn(TestArtifact);
        var artifactEnt = SEntity<XenoArtifactComponent>(artifactUid).AsNullable();

        // Create 3 nodes
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node1, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node2, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEnt, TestArtifactNode, out var node3, false));

        var node1Null = node1!.Value.AsNullable();
        var node2Null = node2!.Value.AsNullable();
        var node3Null = node3!.Value.AsNullable();

        // Add connection: 1 -> 2 -> 3
        _sArtifactSystem.AddEdge(artifactEnt, node1!.Value, node2!.Value, false);
        _sArtifactSystem.AddEdge(artifactEnt, node2!.Value, node3!.Value, false);

        // Make sure our connection is set up
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node1Null, node2Null));
        Assert.That(_sArtifactSystem.NodeHasEdge(artifactEnt, node2Null, node3Null));

        // Remove middle node, severing connections
        _sArtifactSystem.RemoveNode(artifactEnt, node2Null, false);

        // Make sure our connection are properly severed.
        Assert.That(_sArtifactSystem.GetSuccessorNodes(artifactEnt, node1.Value), Is.Empty);
        Assert.That(_sArtifactSystem.GetPredecessorNodes(artifactEnt, node3.Value), Is.Empty);

        // Make sure our matrix is 3x3
        Assert.That(artifactEnt.Comp!.NodeAdjacencyMatrixRows, Is.EqualTo(3));
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
    [RunOnSide(Side.Server)]
    [Description("Checks if the active nodes are properly detected.")]
    public async Task XenoArtifactBuildActiveNodesTest()
    {
        var artifactUid = SSpawn(TestArtifact);
        var artifactEnt = SEntity<XenoArtifactComponent>(artifactUid).AsNullable();

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

        _sArtifactSystem.SetNodeUnlocked(node1!.Value.AsNullable());
        _sArtifactSystem.SetNodeUnlocked(node2!.Value.AsNullable());
        _sArtifactSystem.SetNodeUnlocked(node3!.Value.AsNullable());
        _sArtifactSystem.SetNodeUnlocked(node5!.Value.AsNullable());

        NetEntity[] expectedActiveNodes =
        [
            SEntMan.GetNetEntity(node3!.Value.Owner),
            SEntMan.GetNetEntity(node5!.Value.Owner)
        ];
        Assert.That(artifactEnt.Comp!.CachedActiveNodes, Is.SupersetOf(expectedActiveNodes));
        Assert.That(artifactEnt.Comp.CachedActiveNodes, Has.Count.EqualTo(expectedActiveNodes.Length));
    }

    [Test]
    [RunOnSide(Side.Server)]
    [Description("Checks the shape and number of segments on artifacts with different generation params.")]
    public async Task XenoArtifactGenerateSegmentsTest()
    {
        var artifact1Uid = SSpawn(TestGenArtifactFlat);
        var artifact1Ent = SEntity<XenoArtifactComponent>(artifact1Uid).AsNullable();

        var segments1 = _sArtifactSystem.GetSegments(artifact1Ent!);
        Assert.That(segments1, Has.Count.EqualTo(2));
        Assert.That(segments1[0], Has.Count.EqualTo(1));
        Assert.That(segments1[1], Has.Count.EqualTo(1));

        var artifact2Uid = SSpawn(TestGenArtifactTall);
        var artifact2Ent = SEntity<XenoArtifactComponent>(artifact2Uid).AsNullable();

        var segments2 = _sArtifactSystem.GetSegments(artifact2Ent!);
        Assert.That(segments2, Has.Count.EqualTo(1));
        Assert.That(segments2[0], Has.Count.EqualTo(2));

        var artifact3Uid = SSpawn(TestGenArtifactFull);
        var artifact3Ent = SEntity<XenoArtifactComponent>(artifact3Uid).AsNullable();

        var segments3 = _sArtifactSystem.GetSegments(artifact3Ent!);
        Assert.That(segments3, Has.Count.EqualTo(1));
        Assert.That(segments3.Sum(x => x.Count), Is.EqualTo(6));
        var nodesDepths = segments3[0].Select(x => x.Comp.Depth).ToArray();
        Assert.That(nodesDepths.Distinct().Count(), Is.EqualTo(3));
        var grouped = nodesDepths.ToLookup(x => x);
        Assert.That(grouped[0].Count(), Is.EqualTo(2));
        Assert.That(grouped[1].Count(), Is.GreaterThanOrEqualTo(2)); // tree is attempting sometimes to get wider (so it will look like a tree)
        Assert.That(grouped[2].Count(), Is.LessThanOrEqualTo(2)); // maintain same width or, if we used 3 nodes on previous layer - we only have 1 left!
    }

    [Test]
    [RunOnSide(Side.Server)]
    [Description("Checks that triggering sibling nodes which converge on an unlockable node extends the unlocking time")]
    public async Task XenoArtifactSiblingTriggerTimeTest()
    {
        var artifactUid = SSpawn(TestArtifact);
        var artifactEnt = SEntity<XenoArtifactComponent>(artifactUid);
        var artifactEntNull = artifactEnt.AsNullable();

        // A and B are unlocked sibling branches which both converge on the unlockable node C.
        // To unlock C all of A, B and C have to be triggered during the same unlocking session.
        Assert.That(_sArtifactSystem.AddNode(artifactEntNull, TestArtifactNode, out var nodeA, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEntNull, TestArtifactNode, out var nodeB, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEntNull, TestArtifactNode, out var nodeC, false));

        _sArtifactSystem.AddEdge(artifactEntNull, nodeA!.Value, nodeC!.Value, false);
        _sArtifactSystem.AddEdge(artifactEntNull, nodeB!.Value, nodeC!.Value, false);

        _sArtifactSystem.SetNodeUnlocked(nodeA.Value.AsNullable());
        _sArtifactSystem.SetNodeUnlocked(nodeB.Value.AsNullable());

        var indexA = _sArtifactSystem.GetIndex(artifactEnt, nodeA.Value);
        var indexB = _sArtifactSystem.GetIndex(artifactEnt, nodeB.Value);
        var indexC = _sArtifactSystem.GetIndex(artifactEnt, nodeC.Value);

        // The trigger that starts the unlocking session doesn't extend it.
        _sArtifactSystem.TriggerXenoArtifact(artifactEnt, nodeA.Value, force: true);
        var unlocking = SComp<XenoArtifactUnlockingComponent>(artifactUid);
        Assert.That(unlocking.TriggeredNodeIndexes, Is.EquivalentTo([indexA]));
        var baseEndTime = unlocking.EndTime;

        // Triggering the sibling node B has to extend the unlocking time, even though it is
        // not on the same path as A.
        _sArtifactSystem.TriggerXenoArtifact(artifactEnt, nodeB.Value, force: true);
        Assert.That(unlocking.EndTime - baseEndTime, Is.EqualTo(artifactEnt.Comp.UnlockStateIncrementPerNode));

        // Triggering the unlock target itself has to extend the time as well.
        _sArtifactSystem.TriggerXenoArtifact(artifactEnt, nodeC.Value, force: true);
        Assert.That(unlocking.EndTime - baseEndTime, Is.EqualTo(artifactEnt.Comp.UnlockStateIncrementPerNode * 2));
        Assert.That(unlocking.TriggeredNodeIndexes, Is.EquivalentTo([indexA, indexB, indexC]));

        // With the full required set triggered, C is exactly the node that will get unlocked.
        Assert.That(_sArtifactSystem.TryGetNodeFromUnlockState((artifactUid, unlocking, artifactEnt.Comp), out var unlockable), Is.True);
        Assert.That(unlockable!.Value.Owner, Is.EqualTo(nodeC.Value.Owner));
    }

    [Test]
    [RunOnSide(Side.Server)]
    [Description("Checks that a trigger which makes the unlocking attempt impossible doesn't extend the time")]
    public async Task XenoArtifactImpossibleTriggerTimeTest()
    {
        var artifactUid = SSpawn(TestArtifact);
        var artifactEnt = SEntity<XenoArtifactComponent>(artifactUid);
        var artifactEntNull = artifactEnt.AsNullable();

        // C is unlockable through A, while D is an isolated node which is only unlockable alone.
        Assert.That(_sArtifactSystem.AddNode(artifactEntNull, TestArtifactNode, out var nodeA, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEntNull, TestArtifactNode, out var nodeC, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEntNull, TestArtifactNode, out var nodeD, false));

        _sArtifactSystem.AddEdge(artifactEntNull, nodeA!.Value, nodeC!.Value, false);
        _sArtifactSystem.SetNodeUnlocked(nodeA.Value.AsNullable());

        _sArtifactSystem.TriggerXenoArtifact(artifactEnt, nodeA.Value, force: true);
        var unlocking = SComp<XenoArtifactUnlockingComponent>(artifactUid);
        var baseEndTime = unlocking.EndTime;

        // D is not a predecessor of the unlockable node C, so no unlockable node has both A and D
        // within its required set. This trigger guarantees the unlock will fail - no time is added.
        _sArtifactSystem.TriggerXenoArtifact(artifactEnt, nodeD!.Value, force: true);
        unlocking = SComp<XenoArtifactUnlockingComponent>(artifactUid);
        Assert.That(unlocking.EndTime, Is.EqualTo(baseEndTime));

        Assert.That(_sArtifactSystem.TryGetNodeFromUnlockState((artifactUid, unlocking, artifactEnt.Comp), out _), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    [Description("Checks that a full required trigger set doesn't extend the time when artifexium is applied")]
    public async Task XenoArtifactArtifexiumTimeTest()
    {
        var artifactUid = SSpawn(TestArtifact);
        var artifactEnt = SEntity<XenoArtifactComponent>(artifactUid);
        var artifactEntNull = artifactEnt.AsNullable();

        Assert.That(_sArtifactSystem.AddNode(artifactEntNull, TestArtifactNode, out var nodeA, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEntNull, TestArtifactNode, out var nodeB, false));
        Assert.That(_sArtifactSystem.AddNode(artifactEntNull, TestArtifactNode, out var nodeC, false));

        _sArtifactSystem.AddEdge(artifactEntNull, nodeA!.Value, nodeB!.Value, false);
        _sArtifactSystem.AddEdge(artifactEntNull, nodeB!.Value, nodeC!.Value, false);
        _sArtifactSystem.SetNodeUnlocked(nodeA.Value.AsNullable());

        _sArtifactSystem.TriggerXenoArtifact(artifactEnt, nodeA.Value, force: true);
        var unlocking = SComp<XenoArtifactUnlockingComponent>(artifactUid);
        var baseEndTime = unlocking.EndTime;

        // With artifexium a trigger set one short of the required one is enough to unlock C.
        _sArtifactSystem.SetArtifexiumApplied((artifactUid, unlocking), true);
        Assert.That(_sArtifactSystem.TryGetNodeFromUnlockState((artifactUid, unlocking, artifactEnt.Comp), out var unlockable));
        Assert.That(unlockable!.Value.Owner, Is.EqualTo(nodeB.Value.Owner));

        // Completing the full required set makes the unlock fail under artifexium - no time added.
        _sArtifactSystem.TriggerXenoArtifact(artifactEnt, nodeC.Value, force: true);
        unlocking = SComp<XenoArtifactUnlockingComponent>(artifactUid);
        Assert.That(unlocking.EndTime, Is.EqualTo(baseEndTime));
    }
}
