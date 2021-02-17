using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    /// <summary>
    ///     High-level solution transferring operations like "what happens when a syringe tries to inject this entity."
    /// </summary>
    /// <remarks>
    ///     This interface is most often implemented by using <see cref="SharedSolutionContainerComponent"/>
    ///     and setting the appropriate <see cref="SolutionContainerCaps"/>
    /// </remarks>
    public interface ISolutionInteractionsComponent : IComponent
    {
        //
        // INJECTING
        //

        /// <summary>
        ///     Whether we CAN POTENTIALLY be injected with solutions by items like syringes.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     This should NOT change to communicate behavior like "the container is full".
        ///     Change <see cref="InjectSpaceAvailable"/> to 0 for that.
        /// </para>
        /// <para>
        ///     If refilling is allowed (<see cref="CanRefill"/>) you should also always allow injecting.
        /// </para>
        /// </remarks>
        bool CanInject => false;

        /// <summary>
        ///     The amount of solution space available for injecting.
        /// </summary>
        ReagentUnit InjectSpaceAvailable => ReagentUnit.Zero;

        /// <summary>
        ///     Actually inject reagents.
        /// </summary>
        void Inject(Solution solution)
        {

        }

        //
        // DRAWING
        //

        bool CanDraw => false;
        ReagentUnit DrawAvailable => ReagentUnit.Zero;

        Solution Draw(ReagentUnit amount)
        {
            return new();
        }



        //
        // REFILLING
        //

        bool CanRefill => false;
        ReagentUnit RefillSpaceAvailable => ReagentUnit.Zero;

        void Refill(Solution solution)
        {

        }

        //
        // DRAINING
        //

        bool CanDrain => false;
        ReagentUnit DrainAvailable => ReagentUnit.Zero;

        Solution Drain(ReagentUnit amount)
        {
            return new();
        }
    }
}
