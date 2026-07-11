using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Shared.NodeCrawl;

/// <summary>
/// Handles node-confined movement for an entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(NodeCrawlerMovementSystem))]
public sealed partial class NodeCrawlerMovementComponent : Component
{
    /// <summary>
    /// The current node.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Node;

    /// <summary>
    /// The target node being moved to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? TargetNode;

    /// <summary>
    /// The crawler being carried by this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? HeldCrawler;

    /// <summary>
    /// The required angle to be within for deciding which node to move from the target direction
    /// </summary>
    [DataField]
    public double RequiredAngle = Math.PI / 4f;

    /// <summary>
    /// The contained gas mixture to expose the contained entities' atmos to, if any.
    /// </summary>
    [DataField]
    [Access(typeof(SharedNodeCrawlSystem))]
    public GasMixture? Air;

    /// <summary>
    /// The amount of air to draw into the mover, in liters.
    /// </summary>
    [DataField]
    public float AirVolume = 100f;
}

/// <summary>
/// Event raised when a node crawler arrives at a node entity.
/// </summary>
/// <param name="Node">The arrived-at node.</param>
[ByRefEvent]
public readonly record struct NodeCrawlerArrivedAtNodeEvent(EntityUid Node);
