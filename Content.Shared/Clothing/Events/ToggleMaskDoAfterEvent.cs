using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Events;

/// <summary>
/// This DoAfter event is called when someone attempts to pull up/down a mask.
/// </summary>
/// <param name="state"> The wanted state of the mask. If undefined/null, it simply toggles the mask.</param>
/// <param name="force"> If true, it forces the mask to be toggled even if it cannot be toggled.</param>
[Serializable, NetSerializable]
public sealed partial class ToggleMaskDoAfterEvent : DoAfterEvent
{
    public bool? State;
    public bool Force;
    public bool ByOther;

    public ToggleMaskDoAfterEvent(bool? state, bool force, bool byOther)
    {
        State = state;
        Force = force;
        ByOther = byOther;
    }

    public override DoAfterEvent Clone() => this;
}
