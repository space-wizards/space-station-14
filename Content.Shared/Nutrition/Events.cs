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
/// Raised directed at the food after finishing eating a food before it's deleted.
/// Cancel this if you want to do something special before a food is deleted.
/// If not cancelled trash can be spawned and the food is deleted.
/// Also raised when slicing the last slice of a food.
/// </summary>
[ByRefEvent]
public record struct BeforeFullyEatenEvent(EntityUid? User, bool Cancelled = false)
{
    public void Cancel()
    {
        Cancelled = true;
    }
}

/// <summary>
/// Raised on food after its trash has been spawned, but before it gets deleted.
/// </summary>
[ByRefEvent]
public record struct FoodSpawnedTrashEvent(EntityUid Trash, EntityUid? User);
