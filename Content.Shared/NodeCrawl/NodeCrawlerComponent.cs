using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.NodeCrawl;

/// <summary>
/// Handles entities that can enter and exit node-constrained movement.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedNodeCrawlSystem))]
public sealed partial class NodeCrawlerComponent : Component
{
    /// <summary>
    /// The mover this crawler is currently being carried by, if any
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Mover;

    /// <summary>
    /// Components of entities to reveal while inside a mover
    /// </summary>
    [DataField(readOnly: true)]
    public Type[] RevealedComponents;

    /// <summary>
    /// Whitelist for entities that will be considered as exit nodes.
    /// </summary>
    [DataField]
    public EntityWhitelist? ExitNodes;

    /// <summary>
    /// How long it takes to enter a node.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan EnterDelay = TimeSpan.FromSeconds(0.5f);
}

[Serializable, NetSerializable]
public sealed partial class NodeCrawlEnterDoAfterEvent : SimpleDoAfterEvent;

[ByRefEvent]
public readonly record struct NodeCrawlerStartedCrawlingEvent(Entity<NodeCrawlerMovementComponent> Mover);
