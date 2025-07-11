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
/// <param name="Ingest">Should we actually try and ingest? Or are we just testing if it's even possible </param>
[ByRefEvent]
public record struct CanIngestEvent(bool Handled, EntityUid User, Entity<EdibleComponent?> Ingested, bool Ingest);

/// <summary>
///     Raised directed at the consumer when attempting to ingest something.
/// </summary>
[ByRefEvent]
public record struct IngestionAttemptEvent(bool Cancelled) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; set; }
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
[ByRefEvent]
public record struct BeforeEatenEvent(EntityUid User);

/// <summary>
/// Raised on an entity when it is being made to be eaten.
/// </summary>
/// <param name="User">Who is doing the action?</param>
/// <param name="Target">Who is doing the eating?</param>
/// <param name="Destroy">Whether we should be destroyed after we're done being eaten.</param>
[ByRefEvent]
public record struct EatenEvent(EntityUid User, EntityUid Target, Solution Split, bool Destroy = false);

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
