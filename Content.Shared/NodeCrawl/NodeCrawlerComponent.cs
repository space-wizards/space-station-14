using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared.NodeCrawl;

/// <summary>
/// Handles entities that can enter and exit node-constrained movement.
/// This is used for entities that enter vents themselves (such as mice).
/// If you want to see the entity movement is relayed to check <see cref="NodeCrawlerMovementComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
[Access(typeof(SharedNodeCrawlSystem))]
public sealed partial class NodeCrawlerComponent : Component
{
    /// <summary>
    /// Prototype of the mover spawned for node crawling.
    /// </summary>
    [DataField]
    public EntProtoId MoverProto = "NodeCrawlMover";

    /// <summary>
    /// Whether this crawler can relay to other crawlers.
    /// </summary>
    [DataField]
    public bool Relay;

    /// <summary>
    /// The mover this crawler is currently being carried by, if any
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Mover;

    /// <summary>
    /// Whitelist for entities that will be considered as exit nodes.
    /// </summary>
    [DataField]
    public EntityWhitelist? EntranceNodes = new ()
    {
        Components =
        [
            NodeCrawlVentAccessComponent.ComponentName,
        ]
    };

    /// <summary>
    /// How long it takes to enter a node.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan EnterDelay = TimeSpan.FromSeconds(1f);

    /// <summary>
    /// How long it takes to exit a node.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ExitDelay = TimeSpan.FromSeconds(1f);
}

/// <summary>
/// The DoAfter event that is raised when an entity finishes crawling inside a node.
/// Used to begin node crawling.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class NodeCrawlEnterDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class NodeCrawlExitDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Raised on an entity when it begins node crawling.
/// </summary>
/// <param name="Mover">The mover which handles movement of the entity.</param>
[ByRefEvent]
public readonly record struct NodeCrawlerStartedCrawlingEvent(Entity<NodeCrawlerMovementComponent> Mover);

/// <summary>
/// Raised once an entity exits a node and stops node crawling.
/// </summary>
[ByRefEvent]
public readonly record struct NodeCrawlerStoppedCrawlingEvent;
