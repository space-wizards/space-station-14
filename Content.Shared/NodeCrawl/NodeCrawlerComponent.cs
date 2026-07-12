using Content.Shared.Atmos.Components;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.NodeCrawl;

/// <summary>
/// Handles entities that can enter and exit node-constrained movement.
/// This is used for entities that enter vents themselves (such as mice).
/// If you want to see the entity movement is relayed to check <see cref="NodeCrawlerMovementComponent"/>
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
    // TODO: Replace with ComponentFilter once https://github.com/space-wizards/RobustToolbox/pull/6442 is merged
    [DataField(readOnly: true)]
    public Type[] RevealedComponents = [typeof(PipeAppearanceComponent)];

    /// <summary>
    /// Whitelist for entities that will be considered as entrance nodes.
    /// </summary>
    [DataField]
    public EntityWhitelist? EntranceNodes = new ()
    {
        Components =
        [
            "NodeCrawlVentAccess",
        ]
    };

    /// <summary>
    /// How long it takes to enter a node.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan EnterDelay = TimeSpan.FromSeconds(0.5f);
}

/// <summary>
/// The DoAfter event that is raised when an entity finishes crawling inside a node.
/// Used to begin node crawling.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class NodeCrawlEnterDoAfterEvent : SimpleDoAfterEvent;

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
