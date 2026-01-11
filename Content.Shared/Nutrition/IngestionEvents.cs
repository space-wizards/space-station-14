using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Nutrition;

/// <summary>
/// Raised on an entity that is trying to be ingested to see if it has universal blockers preventing it from being
/// ingested.
/// </summary>
[ByRefEvent]
public record struct IngestibleEvent(bool Cancelled = false);

/// <summary>
/// Raised on an entity with the <see cref="EdibleComponent"/> to check if anything is stopping
/// another entity from consuming the delicious reagents stored inside.
/// </summary>
/// <param name="User">The entity trying to feed us to an entity.</param>
[ByRefEvent]
public record struct EdibleEvent(EntityUid User)
{
    public Entity<SolutionComponent>? Solution = null;

    public TimeSpan Time = TimeSpan.Zero;

    public bool Cancelled;
}

/// <summary>
/// Raised when an entity is trying to ingest an entity to see if it has any component that can ingest it.
/// </summary>
/// <param name="Handled">Did a system successfully ingest this item?</param>
/// <param name="User">The entity that is trying to feed and therefore raising the event</param>
/// <param name="Ingested">What are we trying to ingest?</param>
/// <param name="Ingest">Should we actually try and ingest? Or are we just testing if it's even possible </param>
[ByRefEvent]
public record struct AttemptIngestEvent(EntityUid User, EntityUid Ingested, bool Ingest, bool Handled = false);

/// <summary>
///     Raised on an entity that is consuming another entity to see if there is anything attached to the entity
///     that is preventing it from doing the consumption.
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
///     Raised on an entity that is trying to be digested, aka turned from an entity into reagents.
///     Returns its digestive properties or how difficult it is to convert to reagents.
/// </summary>
/// <remarks>This method is currently needed for backwards compatibility with food and drink component.
///          It also might be needed in the event items like trash and plushies have their edible component removed.
///          There's no way to know whether this event will be made obsolete or not after Food and Drink Components
///          are removed until after a proper body and digestion rework. Oh well!
/// </remarks>
[ByRefEvent]
public record struct IsDigestibleEvent()
{
    public bool Digestible = false;

    public bool SpecialDigestion = false;

    // If this is true, SpecialDigestion will be ignored
    public bool Universal = false;

    // If it requires special digestion then it has to be digestible...
    public void AddDigestible(bool special)
    {
        SpecialDigestion = special;
        Digestible = true;
    }

    // This should only be used for if you're trying to drink pure reagents from a puddle or cup or something...
    public void UniversalDigestion()
    {
        Universal = true;
        Digestible = true;
    }
}

/// <summary>
/// Do After Event for trying to put food solution into stomach entity.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class EatingDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// We use this to determine if an entity should abort giving up its reagents at the last minute,
/// as well as specifying how much of its reagents it should give up including minimums and maximums.
/// If minimum exceeds the  maximum, the event will abort.
/// </summary>
/// <param name="Min">The minimum amount we can transfer.</param>
/// <param name="Max">The maximum amount we can transfer.</param>
/// <param name="Solution">The solution we are transferring.</param>
[ByRefEvent]
public record struct BeforeIngestedEvent(FixedPoint2 Min, FixedPoint2 Max, Solution? Solution)
{
    // How much we would like to transfer, gets clamped by Min and Max.
    public FixedPoint2 Transfer;

    // Whether this event, and therefore eat attempt, should be cancelled.
    public bool Cancelled;

    // When and if we eat this solution, should we actually remove solution or should it get replaced?
    // This bool basically only exists because of stackable system.
    public bool Refresh;

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
/// Raised on an entity while it is eating
/// </summary>
/// <param name="Food">The item being ingested</param>
/// <param name="Split">The solution being ingested</param>
/// <param name="ForceFed">Whether or not we're being forced</param>
[ByRefEvent]
public record struct IngestingEvent(EntityUid Food, Solution Split, bool ForceFed);

/// <summary>
/// Raised on an entity when it is being made to be eaten.
/// </summary>
/// <param name="User">Who is doing the action?</param>
/// <param name="Target">Who is doing the eating?</param>
/// <param name="Split">The solution we're currently eating.</param>
/// <param name="ForceFed">Whether we're being fed by someone else, checkec enough I might as well pass it.</param>
[ByRefEvent]
public record struct IngestedEvent(EntityUid User, EntityUid Target, Solution Split, bool ForceFed)
{
    // Should we destroy the ingested entity?
    public bool Destroy;

    // Has this eaten event been handled? Used to prevent duplicate flavor popups and sound effects.
    public bool Handled;

    // Should we try eating again?
    public bool Repeat;
}

/// <summary>
/// Raised directed at the food after finishing eating it and before it's deleted.
/// </summary>
[ByRefEvent]
public readonly record struct FullyEatenEvent(EntityUid User)
{
    /// <summary>
    /// The entity that ate the food.
    /// </summary>
    public readonly EntityUid User = User;
}

/// <summary>
/// Returns a list of Utensils that can be used to consume the entity, as well as a list of required types.
/// </summary>
[ByRefEvent]
public record struct GetUtensilsEvent()
{
    public UtensilType Types = UtensilType.None;

    public UtensilType RequiredTypes = UtensilType.None;

    // Forces you to add to both lists if a utensil is required.
    public void AddRequiredTypes(UtensilType type)
    {
        RequiredTypes |= type;
        Types |= type;
    }
}

/// <summary>
/// Tries to get the best fitting edible type for an entity.
/// </summary>
[ByRefEvent]
public record struct GetEdibleTypeEvent
{
    public ProtoId<EdiblePrototype>? Type { get; private set; }

    public void SetPrototype([ForbidLiteral] ProtoId<EdiblePrototype> proto)
    {
        Type = proto;
    }
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
