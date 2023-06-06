using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Gatherable;

[Serializable, NetSerializable]
public sealed class GatherableDoAfterEvent : SimpleDoAfterEvent
{
    /// <summary>
    ///     Whether the gatherable action is being performed by hand.
    /// </summary>
    [DataField("byHand")]
    public bool ByHand = false;

    /// <summary>
    ///     Creates a new set of GatherableDoAfterEvent arguments.
    /// </summary>
    /// <param name="byHand">Whether the gatherable action is being performed by hand.</param>
    public GatherableDoAfterEvent(bool byHand = false)
    {
        ByHand = byHand;
    }
}
