using Content.Shared.Interaction;
using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Allows an entity with an inventory or hands to steal items from nearby entities.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InventoryVacuumComponent : Component
{
    /// <summary>
    /// The maximum range at which the vacuum can steal items.
    /// </summary>
    [DataField]
    public float StealRange = SharedInteractionSystem.InteractionRange;

    /// <summary>
    /// Chance to steal an item. Attempted once every <see cref="StealAttemptCooldown"/>.
    /// </summary>
    [DataField]
    public float StealChance = 0.1f;

    /// <summary>
    /// How long to wait between steal attempts, regardless of success or failure.
    /// </summary>
    [DataField]
    public TimeSpan StealAttemptCooldown = TimeSpan.FromMinutes(1);

    [DataField]
    public TimeSpan NextStealAttempt = TimeSpan.Zero;

    /// <summary>
    /// A whitelist of inventory slots that the vacuum can steal from. If empty, it can steal from any slot.
    /// </summary>
    [DataField]
    public HashSet<string> StealSlotWhitelist = ["hand", "pocket1", "pocket2", "id"];

    /// <summary>
    /// Whether the vacuum should check line of sight before stealing.
    /// If true, the entity will only steal from entities that it can see and access.
    /// </summary>
    [DataField]
    public bool ShouldCheckLineOfSight = true;
}
