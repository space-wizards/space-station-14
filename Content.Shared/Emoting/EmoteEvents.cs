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
/// An event raised just before an emote is performed, providing systems with an opportunity to cancel the emote's performance.
/// </summary>
[ByRefEvent]
public sealed class BeforeEmoteEvent : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public readonly EntityUid Source;
    public readonly EmotePrototype Emote;

    /// <summary>
    ///     The equipment that is blocking emoting. Should only be non-null if the event was canceled.
    /// </summary>
    public EntityUid? Blocker = null;

    public BeforeEmoteEvent(EntityUid source, EmotePrototype emote)
    {
        Source = source;
        Emote = emote;
    }

    public SlotFlags TargetSlots => SlotFlags.All;
}
