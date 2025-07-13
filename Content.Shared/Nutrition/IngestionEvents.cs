using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Nutrition;

/// <summary>
/// Raised on an entity to see if anything would prevent it from being Ingested no matter the method.
/// </summary>
[ByRefEvent]
public record struct IngestibleEvent(bool Cancelled = false);

/// <summary>
/// Raised on an entity with the <see cref="EdibleComponent"/> to check if anything is stopping
/// another entity from consuming the delicious reagents stored inside.
/// </summary>
/// <param name="User">The entity trying to feed us to an entity.</param>
/// <param name="Destroy">Will this entity be destroyed when it's eaten?</param>
/// <param name="Cancelled">If something prevented us from accessing the reagents, the event is cancelled</param>
[ByRefEvent]
public record struct EdibleEvent(EntityUid User, bool Destroy, bool Cancelled = false);

/// <summary>
/// Raised when an entity is trying to ingest an entity to see if it has any component that can ingest it.
/// </summary>
/// <param name="Handled">Did a system successfully ingest this item?</param>
/// <param name="User">The entity that is trying to feed and therefore raising the event</param>
/// <param name="Ingested">What are we trying to ingest?</param>
/// <param name="Ingest">Should we actually try and ingest? Or are we just testing if it's even possible </param>
[ByRefEvent]
public record struct CanIngestEvent(EntityUid User, Entity<EdibleComponent?> Ingested, bool Ingest, bool Handled = false);

/// <summary>
///     Raised directed at the consumer when attempting to ingest something.
/// </summary>
[ByRefEvent]
public record struct IngestionAttemptEvent(SlotFlags TargetSlots, bool Cancelled = false) : IInventoryRelayEvent
{
    /// <summary>
    ///     The equipment that is blocking consumption. Should only be non-null if the event was canceled.
    /// </summary>
    public EntityUid? Blocker = null;
}

/// <summary>
/// Do After Event for trying to put food solution into stomach entity.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class EatingDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// We use this to
/// </summary>
/// <param name="User"></param>
/// <param name="Min">The minimum amount we can transfer.</param>
/// <param name="Max">The maximum amount we can transfer.</param>
/// <param name="Solution">The solution we are transferring.</param>
[ByRefEvent]
public record struct BeforeEatenEvent(EntityUid User, EntityUid Target, FixedPoint2 Min, FixedPoint2 Max, Solution? Solution)
{
    // How much we would like to transfer, gets clamped by Min and Max.
    public FixedPoint2 Transfer;

    // Whether this event, and therefore eat attempt, should be cancelled.
    public bool Cancelled;

    public bool TryNewMinimum(FixedPoint2 newMin)
    {
        if (newMin > Max)
            return false;

        Min = newMin;
        return true;
    }

    public bool TryNewMaximum(FixedPoint2 newMax)
    {
        if (newMax < Min)
            return false;

        Min = newMax;
        return true;
    }
}

/// <summary>
/// Raised on an entity when it is being made to be eaten.
/// </summary>
/// <param name="User">Who is doing the action?</param>
/// <param name="Target">Who is doing the eating?</param>
/// <param name="Split">The solution we're currently eating.</param>
/// <param name="ForceFed">Whether we're being fed by someone else, checkec enough I might as well pass it.</param>
[ByRefEvent]
public record struct IngestSolutionEvent(EntityUid User, EntityUid Target, Solution Split, bool ForceFed)
{
    // Should we refill the solution now that we've eaten it?
    // This bool basically only exists because of stackable system.
    public bool Refresh;

    // Should we destroy the ingested entity?
    public bool Destroy;

    // Has this eaten event been handled? Used to prevent duplicate flavor popups and sound effects.
    public bool Handled;
};

// TODO: This can probably go.
/// <summary>
/// Raised directed at the food after finishing eating a food before it's deleted.
/// Cancel this if you want to do something special before a food is deleted.
/// </summary>
public sealed class BeforeFullyEatenEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The person that ate the food.
    /// </summary>
    public EntityUid User;
}

// TODO: This should go if possible or at least be retooled
/// <summary>
/// Raised directed at the food after finishing eating it and before it's deleted.
/// </summary>
[ByRefEvent]
public readonly record struct AfterFullyEatenEvent(EntityUid User)
{
    /// <summary>
    /// The entity that ate the food.
    /// </summary>
    public readonly EntityUid User = User;
}

/// <summary>
/// Raised directed at the food being sliced before it's deleted.
/// Cancel this if you want to do something special before a food is deleted.
/// </summary>
public sealed class BeforeFullySlicedEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The person slicing the food.
    /// </summary>
    public EntityUid User;
}
