using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Nutrition;

/// <summary>
/// Raised on an entity to see if anything would prevent it from being Ingested no matter the method.
/// </summary>
[ByRefEvent]
public record struct IngestibleEvent(bool Cancelled);

/// <summary>
/// Raised on an entity with the <see cref="EdibleComponent"/> to check if anything is stopping
/// another entity from consuming the delicious reagents stored inside.
/// </summary>
/// <param name="User">The entity trying to feed us to an entity.</param>
/// <param name="Destroy">Will this entity be destroyed when it's eaten?</param>
/// <param name="Cancelled">If something prevented us from accessing the reagents, the event is cancelled</param>
[ByRefEvent]
public record struct EdibleEvent(EntityUid User, bool Destroy, bool Cancelled);

/// <summary>
/// Raised when an entity is trying to ingest an entity to see if it has any component that can ingest it.
/// </summary>
/// <param name="Handled">Did a system successfully ingest this item?</param>
/// <param name="User">The entity that is trying to feed and therefore raising the event</param>
/// <param name="Ingested">What are we trying to ingest?</param>
/// <param name="Solution">The solution we're attempting to ingest. </param> // TODO: Maybe get the solution during this event...
[ByRefEvent]
public record struct CanIngestEvent(bool Handled, EntityUid User, Entity<EdibleComponent?> Ingested, Solution? Solution);

/// <summary>
///     Raised directed at the consumer when attempting to ingest something.
/// </summary>
[ByRefEvent]
public record struct IngestionAttemptEvent(bool Cancelled) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; set; } = SlotFlags.HEAD | SlotFlags.MASK;
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
/// Raised on an entity when it successfully eats an item of food
/// </summary>
/// <param name="Food">The food item in question being eaten</param>
[ByRefEvent]
public record struct EatenEvent(EntityUid Food);

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
