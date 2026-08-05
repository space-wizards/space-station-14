using System.Numerics;
using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.NodeCrawl;

/// <summary>
/// Handles node-confined movement for an entity.
/// This is the entity movement is relayed to inside a node.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(NodeCrawlerMovementSystem))]
public sealed partial class NodeCrawlerMovementComponent : Component
{
    /// <summary>
    /// The current node.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Node;

    /// <summary>
    /// Direction the player is currently trying to move in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 MoveVector;

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

    /// <summary>
    /// Minimum time between traversal sounds.
    /// </summary>
    [DataField]
    public TimeSpan TraversalSoundDelay = TimeSpan.FromSeconds(2f);

    /// <summary>
    /// When the traversal sound was last played, for throttling.
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan LastTraversalSound;

    /// <summary>
    /// Sound played periodically while moving through nodes.
    /// </summary>
    [DataField]
    public SoundCollectionSpecifier TraversalSound { get; set; } = new("VentClaw", AudioParams.Default.WithVolume(5f));

    /// <summary>
    /// The pipe layer the crawler is currently on.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int CurrentLayer;

    /// <summary>
    /// Minimum time between layer switches, to prevent flicker.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public TimeSpan LayerSwitchCooldown = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// When the layer was last changed, for throttling.
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan LastLayerSwitch;
}

/// <summary>
/// Event raised when a node crawler arrives at a node entity.
/// </summary>
/// <param name="Node">The arrived-at node.</param>
[ByRefEvent]
public readonly record struct NodeCrawlerArrivedAtNodeEvent(EntityUid Node, Entity<NodeCrawlerMovementComponent> Movement);

/// <summary>
/// Raised on a pipe node to check whether a crawler can move from <paramref name="From"/> to <paramref name="To"/>.
/// </summary>
[ByRefEvent]
public record struct NodeCrawlCanTraverseEvent(Entity<NodeCrawlerMovementComponent> Movement, EntityUid From, EntityUid To)
{
    public bool Cancelled;
}

/// <summary>
/// Raised on a manifold before the crawler moves, to allow layer switching.
/// </summary>
[ByRefEvent]
public record struct NodeCrawlBeforeMoveEvent(Entity<NodeCrawlerMovementComponent> Movement, Vector2 MoveVector)
{
    public bool Handled;
}

