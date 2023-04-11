using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Chemistry.Components;

namespace Content.Shared.Nutrition;

/// <summary>
///     Do after even for food and drink.
/// </summary>
[Serializable, NetSerializable]
public sealed class ConsumeDoAfterEvent : DoAfterEvent
{
    [DataField("solution", required: true)]
    public readonly string Solution = default!;

    [DataField("flavorMessage", required: true)]
    public readonly string FlavorMessage = default!;

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
public sealed class VapeDoAfterEvent : DoAfterEvent
{
    [DataField("solution", required: true)]
    public readonly Solution Solution = default!;

    [DataField("forced", required: true)]
    public readonly bool Forced = default!;

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
