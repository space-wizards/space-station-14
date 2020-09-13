using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    /// <summary>
    ///     These are the defined capabilities of a container of a solution.
    /// </summary>
    [Flags]
    [Serializable, NetSerializable]
    public enum SolutionContainerCaps
    {
        None = 0, 

        /// <summary>
        /// Can solutions be added into the container?
        /// </summary>
        AddTo = 1,

        /// <summary>
        /// Can solutions be removed from the container?
        /// </summary>
        RemoveFrom = 2,

        /// <summary>
        /// Allows the container to be placed in a <c>ReagentDispenserComponent</c>.
        /// <para>Otherwise it's considered to be too large or the improper shape to fit.</para>
        /// <para>Allows us to have obscenely large containers that are harder to abuse in chem dispensers
        /// since they can't be placed directly in them.</para>
        /// </summary>
        FitsInDispenser = 4,

        /// <summary>
        /// Can people examine the solution in the container or is it impossible to see?
        /// </summary>
        NoExamine = 8,
    }
}
