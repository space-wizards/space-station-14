using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Temperature
{
    /// <summary>
    ///     Directed event raised on entities to query whether they're "hot" or not.
    ///     For example, a lit welder or matchstick would be hot, etc.
    /// </summary>
    public class IsHotEvent : EntityEventArgs
    {
        public bool IsHot { get; set; } = false;
    }
}
