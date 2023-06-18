using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Extinguisher.Events;
[Serializable, NetSerializable]
public sealed class CoolingDoAfterEvent : DoAfterEvent
{
    [DataField("water")]
    public readonly float Water;

    /// <summary>
    ///     Entity that the wrapped do after event will get directed at. If null, event will be broadcast.
    /// </summary>
    [DataField("target")]
    public readonly EntityUid? OriginalTarget;

    [DataField("wrappedEvent")]
    public readonly DoAfterEvent WrappedEvent = default!;

    private CoolingDoAfterEvent()
    {
    }

    public CoolingDoAfterEvent(float water, DoAfterEvent wrappedEvent, EntityUid? originalTarget)
    {
        Water = water;
        WrappedEvent = wrappedEvent;
        OriginalTarget = originalTarget;
    }

    public override DoAfterEvent Clone()
    {
        var evClone = WrappedEvent.Clone();

        // Most DoAfter events are immutable
        return evClone == WrappedEvent ? this : new CoolingDoAfterEvent(Water, evClone, OriginalTarget);
    }
}
