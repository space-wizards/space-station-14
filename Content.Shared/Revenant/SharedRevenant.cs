using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Revenant;

public sealed class RevenantDefileActionEvent : InstantActionEvent { }
public sealed class RevenantOverloadLightsActionEvent : InstantActionEvent { }
public sealed class RevenantMalfunctionActionEvent : InstantActionEvent { }

[NetSerializable, Serializable]
public enum RevenantVisuals
{
    Corporeal,
    Stunned,
    Harvesting,
}
