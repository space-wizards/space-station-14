#nullable enable
using System;
using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    /// <summary>
    ///     Define common interaction behaviors for <see cref="SharedSolutionContainerComponent"/>
    /// </summary>
    /// <seealso cref="ISolutionInteractionsComponent"/>
    [Flags]
    [Serializable, NetSerializable]
    public enum SolutionContainerCaps : ushort
    {
        None = 0,

        /// <summary>
        ///     Reagents can be added with syringes.
        /// </summary>
        Injectable = 1 << 0,

        /// <summary>
        ///     Reagents can be removed with syringes.
        /// </summary>
        Drawable = 1 << 1,

        /// <summary>
        ///     Reagents can be easily added via all reagent containers.
        ///     Think pouring something into another beaker or into the gas tank of a car.
        /// </summary>
        Refillable = 1 << 2,

        /// <summary>
        ///     Reagents can be easily removed through any reagent container.
        ///     Think pouring this or draining from a water tank.
        /// </summary>
        Drainable = 1 << 3,

        /// <summary>
        ///     The contents of the solution can be examined directly.
        /// </summary>
        CanExamine = 1 << 4,

        /// <summary>
        /// Allows the container to be placed in a <c>ReagentDispenserComponent</c>.
        /// <para>Otherwise it's considered to be too large or the improper shape to fit.</para>
        /// <para>Allows us to have obscenely large containers that are harder to abuse in chem dispensers
        /// since they can't be placed directly in them.</para>
        /// </summary>
        FitsInDispenser = 1 << 5,

        OpenContainer = Refillable | Drainable | CanExamine
    }

    public static class SolutionContainerCapsHelpers
    {
        public static bool HasCap(this SolutionContainerCaps cap, SolutionContainerCaps other)
        {
            return (cap & other) == other;
        }
    }
}
