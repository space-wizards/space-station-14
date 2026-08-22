using Content.Shared.Actions;
using Content.Shared.RatKing.Components;

namespace Content.Shared.RatKing.Events;

public sealed partial class RatKingRaiseArmyActionEvent : InstantActionEvent;

public sealed partial class RatKingDomainActionEvent : InstantActionEvent;

public sealed partial class RatKingOrderActionEvent : InstantActionEvent
{
    /// <summary>
    /// The type of order being given.
    /// </summary>
    [DataField(required: true)]
    public RatKingOrderType Type;
}
