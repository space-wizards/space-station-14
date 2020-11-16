namespace Content.Shared.Actions
{
    /// <summary>
    /// Every possible action. Corresponds to actionType in action prototypes.
    /// </summary>
    public enum ActionType : byte
    {
        Error,
        DebugInstant,
        DebugToggle,
        DebugTargetPointLocal,
        DebugTargetPointMap
    }
}
