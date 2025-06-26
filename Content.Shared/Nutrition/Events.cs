using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Nutrition;

/// <summary>
///     Do after even for food and drink.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ConsumeDoAfterEvent : DoAfterEvent
{
    [DataField("solution", required: true)]
    public string Solution = default!;

    [DataField("flavorMessage", required: true)]
    public string FlavorMessage = default!;

    private ConsumeDoAfterEvent()
    {
    }

    public ConsumeDoAfterEvent(string solution, string flavorMessage)
    {
        Solution = solution;
        FlavorMessage = flavorMessage;
    }

    public override DoAfterEvent Clone() => this;
}

/// <summary>
///     Raised on the entity successfully consuming something, after the solution transfer but before deletion.
/// </summary>
[ByRefEvent]
public record struct SuccessfulConsumptionEvent
{
    /// <summary>
    /// The solution that was consumed.
    /// </summary>
    public Solution Solution = default!;

    /// <summary>
    /// The entity that was consumed.
    /// </summary>
    public EntityUid ConsumedEntity = default!;

    /// <summary>
    /// If the consumed entity is considered "fully" consumed, i.e. the entity is deleted/stack reduced.
    /// </summary>
    public bool FullConsumption;

    public SuccessfulConsumptionEvent(Solution solution, EntityUid consumedEntity, bool fullConsumption)
    {
        Solution = solution;
        ConsumedEntity = consumedEntity;
        FullConsumption = fullConsumption;
    }
}

/// <summary>
///     Do after event for vape.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class VapeDoAfterEvent : DoAfterEvent
{
    [DataField("solution", required: true)]
    public Solution Solution = default!;

    [DataField("forced", required: true)]
    public bool Forced = default!;

    private VapeDoAfterEvent()
    {
    }

    public VapeDoAfterEvent(Solution solution, bool forced)
    {
        Solution = solution;
        Forced = forced;
    }

    public override DoAfterEvent Clone() => this;
}

/// <summary>
/// Raised before food is sliced
/// </summary>
[ByRefEvent]
public record struct SliceFoodEvent();

/// <summary>
/// is called after a successful attempt at slicing food.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SliceFoodDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
///    Raised on FoodSequence start element entity when new ingredient is added to FoodSequence
/// </summary>
public record struct FoodSequenceIngredientAddedEvent(EntityUid Start, EntityUid Element, ProtoId<FoodSequenceElementPrototype> Proto, EntityUid? User = null);
