namespace Content.Shared.Actions.Events;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ActionUpgradeEvent : EntityEventArgs
{
    public int Level;
    public EntityUid? ActionId;
    // TODO: Add level (check if able first)
    // TODO: Replace current action with new one
    // TODO: Preserve ordering of actions
}

public sealed partial class ActionUpgradeIncreaseEvent : ActionUpgradeEvent
{
    public int? Charges;
    public int? UsesBeforeDelay;
}

//   Decrease Event(charges to null)
public sealed partial class ActionUpgradeDecreaseEvent : ActionUpgradeEvent
{
    public int? Charges;
    public TimeSpan Delay;
}

// TODO: Add support for changing events
//   ChangeEvent,Event? See the comp
