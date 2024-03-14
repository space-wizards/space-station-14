using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
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
/// Raised on an entity when it tries to make a sound while eating something.
/// Cancelling will stop the sound from being played.
/// </summary>
/// <param name="FoodEnt">the entity being eaten</param>
[ByRefEvent]
public record struct AttemptMakeEatingSoundEvent(EntityUid FoodEnt)
{
    public bool Cancelled;
}

/// <summary>
/// Raised on an entity when it tries to make a sound while drinking something.
/// Cancelling will stop the sound from being played.
/// </summary>
/// <param name="DrinkEnt">the entity being drunk</param>
[ByRefEvent]
public record struct AttemptMakeDrinkingSoundEvent(EntityUid DrinkEnt)
{
    public bool Cancelled;
}
