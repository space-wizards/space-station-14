namespace Content.Shared.Actions
{
    /// <summary>
    /// Every possible action. Corresponds to actionType in action prototypes.
    /// </summary>
    public enum ActionType : byte
    {
        Error,
        HumanScream,
        Disarm,
        DebugInstant,
        DebugToggle,
        DebugTargetPoint,
        DebugTargetPointRepeat,
        DebugTargetEntity,
        DebugTargetEntityRepeat
    }

    /// <summary>
    /// Every possible item action. Corresponds to actionType in itemAction prototypes.
    /// </summary>
    public enum ItemActionType : byte
    {
        Error,
        ToggleInternals,
        ToggleLight,
        DebugInstant,
        DebugToggle,
        DebugTargetPoint,
        DebugTargetPointRepeat,
        DebugTargetEntity,
        DebugTargetEntityRepeat
    }
}
