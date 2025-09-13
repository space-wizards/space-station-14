using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Ronstation.Vampire;

/// <summary>
/// Action event for Feed, someone has tried to feed on someone.
/// </summary>
public sealed partial class VampireFeedActionEvent : EntityTargetActionEvent;

/// <summary>
/// A player has successfully targeted someone with the VampireFeedAction and is beginning to feed on them.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class VampireFeedDoAfterEvent : DoAfterEvent
{
    public VampireFeedDoAfterEvent()
    {
    }
    public override DoAfterEvent Clone() => this;
}

