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

        /// <summary>
        /// Allows the container to be placed in a <c>ReagentDispenserComponent</c>.
        /// <para>Otherwise it's considered to be too large or the improper shape to fit.</para>
        /// <para>Allows us to have obscenely large containers that are harder to abuse in chem dispensers
        /// since they can't be placed directly in them.</para>
        /// </summary>
        FitsInDispenser = 16, 
    }
}
