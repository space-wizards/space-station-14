namespace Content.Shared.Actions.Events;

public sealed partial class ActionUpgradeEvent : EntityEventArgs
{
    public int NewLevel;
    public EntityUid? ActionId;

    public ActionUpgradeEvent(int newLevel, EntityUid? actionId)
    {
        NewLevel = newLevel;
        ActionId = actionId;
    }
}

