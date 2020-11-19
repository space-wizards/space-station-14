#nullable enable
using System;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    public abstract class SharedSolutionContainerComponent : Component
    {
        public override string Name => "SolutionContainer";

        /// <inheritdoc />
        public sealed override uint? NetID => ContentNetIDs.SOLUTION;

        private Solution _solution = new Solution();
        private ReagentUnit _maxVolume;
        private Color _substanceColor;

        /// <summary>
        ///     The contained solution.
        /// </summary>
        [ViewVariables]
        public Solution Solution
        {
            get => _solution;
            set
            {
                if (_solution == value)
                {
                    return;
                }

                _solution = value;
                Dirty();
            }
        }

        /// <summary>
        ///     The total volume of all the of the reagents in the container.
        /// </summary>
        [ViewVariables]
        public ReagentUnit CurrentVolume => Solution.TotalVolume;

        /// <summary>
        ///     The maximum volume of the container.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit MaxVolume
        {
            get => _maxVolume;
            set
            {
                if (_maxVolume == value)
                {
                    return;
                }

                _maxVolume = value;
                Dirty();
            }
        }

        /// <summary>
        ///     The current blended color of all the reagents in the container.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public virtual Color SubstanceColor
        {
            get => _substanceColor;
            set
            {
                if (_substanceColor == value)
                {
                    return;
                }

                _substanceColor = value;
                Dirty();
            }
        }

        /// <summary>
        ///     The current capabilities of this container (is the top open to pour? can I inject it into another object?).
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public SolutionContainerCaps Capabilities { get; set; }

        public abstract bool CanAddSolution(Solution solution);

        public abstract bool TryAddSolution(Solution solution, bool skipReactionCheck = false, bool skipColor = false);

        public abstract bool TryRemoveReagent(string reagentId, ReagentUnit quantity);

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new SolutionContainerComponentState(Solution);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not SolutionContainerComponentState state)
            {
                return;
            }

            _solution = state.Solution;
        }
    }

    [Serializable, NetSerializable]
    public class SolutionContainerComponentState : ComponentState
    {
        public readonly Solution Solution;

        public SolutionContainerComponentState(Solution solution) : base(ContentNetIDs.SOLUTION)
        {
            Solution = solution;
        }
    }
}
