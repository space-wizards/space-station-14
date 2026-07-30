using Content.Shared.Actions;

namespace Content.Shared.Teleportation;

public sealed partial class TeleportActionEvent : WorldTargetActionEvent
{
    [DataField]
    public bool StopBeingPulled;

    [DataField]
    public bool StopPulling;
}
