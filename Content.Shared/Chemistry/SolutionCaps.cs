using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    /// <summary>
    ///     These are the defined capabilities of a container of a solution.
    /// </summary>
    [Flags]
    [Serializable, NetSerializable]
    public enum SolutionCaps
    {
        None = 0,

        PourIn = 1,
        PourOut = 2,

        Injector = 4,
        Injectable = 8,
    }
}
