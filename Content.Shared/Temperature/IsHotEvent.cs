namespace Content.Shared.Temperature
{
    /// <summary>
    ///     Directed event raised on entities to query whether they're "hot" or not.
    ///     For example, a lit welder or matchstick would be hot, etc.
    /// </summary>
    public sealed partial class IsHotEvent : EntityEventArgs
    {
        public bool IsHot { get; set; } = false;
    }
}

