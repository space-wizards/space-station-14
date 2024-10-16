using Content.Shared.Chat.Prototypes;
using Content.Shared.Inventory;

namespace Content.Shared.Emoting;

public sealed class EmoteAttemptEvent : CancellableEntityEventArgs
{
    public EmoteAttemptEvent(EntityUid uid)
    {
        Uid = uid;
    }

    public EntityUid Uid { get; }
}

/// <summary>
/// An event which is raised to determine if the entity's ability to perform certain emotes is blocked
/// </summary>
[ByRefEvent]
public record struct GetEmoteBlockersEvent() : IInventoryRelayEvent
{
    /// <summary>
    /// Which categories of emotes are generally blocked.
    /// </summary>
    public HashSet<EmoteCategory> BlockedCategories = new HashSet<EmoteCategory>();

    /// <summary>
    /// IDs of which emotes are specifically blocked.
    /// </summary>
    public HashSet<string> BlockedEmotes = new HashSet<string>();

    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.All;

    public bool ShouldBlock(EmotePrototype emote) => BlockedCategories.Contains(emote.Category) || BlockedEmotes.Contains(emote.ID);
}
