using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;

namespace Content.Shared.Interaction;

[Prototype]
public sealed partial class InteractionTypePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = string.Empty;
}

public abstract partial class TrackedInteractionRequirementComponent<T> : Component where T: InteractionRequirementComponent, new()
{
    /// <summary>
    /// Contains entities that have interacted with this entity.
    /// Used to signal requirement state change.
    /// </summary>
    public HashSet<EntityUid> Interactions = new();
}

public abstract partial class InteractionRequirementComponent : Component
{
    /// <summary>
    /// List of interactions this requirement is used for,
    /// if interaction is not in this set, requirement wont be checked.
    /// In case of null, requirement is applied for all interactions.
    /// </summary>
    /// <remarks>
    /// Defaults to null
    /// </remarks>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<InteractionTypePrototype>>? RequiredFor = null;

    /// <summary>
    /// Contains entities that have interacted with this entity.
    /// Used to signal requirement state change.
    /// </summary>
    public HashSet<EntityUid> Interactions = new();
}

/// <summary>
/// Rised by entity system on entity with conditions to check if interaction can be performed
/// </summary>
[ByRefEvent]
public struct ConditionalInteractionAttemptEvent(EntityUid source, ProtoId<InteractionTypePrototype> interactionType)
{
    public bool Cancelled = false;

    /// <summary>
    /// The entity that iniciated the interaction
    /// </summary>
    public EntityUid Source = source;

    /// <summary>
    /// Suffix to LocId in case entity system wants to show some error message.
    /// Not empty when <see cref="Cancelled"/> is true
    /// </summary>
    public string FailureSuffix = string.Empty;

    public ProtoId<InteractionTypePrototype> InteractionType = interactionType;
}

/// <summary>
/// Rised by entity system on entity with conditions if interaction is permited
/// </summary>
[ByRefEvent]
public struct ConditionalInteractionEvent(EntityUid source, ProtoId<InteractionTypePrototype> interactionType)
{
    /// <summary>
    /// The entity that iniciated the interaction
    /// </summary>
    public EntityUid Source = source;

    public ProtoId<InteractionTypePrototype> InteractionType = interactionType;
}

/// <summary>
/// Rised by entity system on entity with conditions if interaction is permited
/// </summary>
[ByRefEvent]
public struct ConditionalInteractionEndEvent(EntityUid source, ProtoId<InteractionTypePrototype> interactionType)
{
    /// <summary>
    /// The entity that iniciated the interaction
    /// </summary>
    public EntityUid Source = source;

    public ProtoId<InteractionTypePrototype> InteractionType = interactionType;
}

/// <summary>
/// Rised by specific interaction condition system, to inform that condition has changed.
/// Entity systems should subscribe to this, to abort interactions in progress.
/// </summary>
[ByRefEvent]
public struct InteractionConditionChangedEvent(EntityUid source, bool allow)
{
    /// <summary>
    /// If true, interaction is long gone, and should be forgot about
    /// </summary>
    public bool Cancelled = false;

    /// <summary>
    /// Suffix to LocId in case entity system wants to show some error message.
    /// Not empty when <see cref="Allow"/> is false
    /// </summary>
    public string FailureSuffix = string.Empty;

    public bool Allow = allow;

    /// <summary>
    /// The entity that iniciated the interaction
    /// </summary>
    public EntityUid Source = source;
}
