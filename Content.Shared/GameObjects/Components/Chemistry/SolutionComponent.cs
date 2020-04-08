using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    public class SolutionComponent : Component
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        [ViewVariables]
        protected Solution ContainedSolution = new Solution();
        private ReagentUnit _maxVolume;
        private SolutionCaps _capabilities;

        /// <summary>
        /// Triggered when the solution contents change.
        /// </summary>
        public event Action SolutionChanged;

        /// <summary>
        ///     The maximum volume of the container.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit MaxVolume
        {
            get => _maxVolume;
            set => _maxVolume = value; // Note that the contents won't spill out if the capacity is reduced.
        }

        /// <summary>
        ///     The total volume of all the of the reagents in the container.
        /// </summary>
        [ViewVariables]
        public ReagentUnit CurrentVolume => ContainedSolution.TotalVolume;

        /// <summary>
        ///     The volume without reagents remaining in the container.
        /// </summary>
        [ViewVariables]
        public ReagentUnit EmptyVolume => MaxVolume - CurrentVolume;

        /// <summary>
        ///     The current blended color of all the reagents in the container.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Color SubstanceColor { get; private set; }

        /// <summary>
        ///     The current capabilities of this container (is the top open to pour? can I inject it into another object?).
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public SolutionCaps Capabilities
        {
            get => _capabilities;
            set => _capabilities = value;
        }

        public IReadOnlyList<Solution.ReagentQuantity> ReagentList => ContainedSolution.Contents;

        /// <summary>
        /// Shortcut for Capabilities PourIn flag to avoid binary operators.
        /// </summary>
        public bool CanPourIn => (Capabilities & SolutionCaps.PourIn) != 0;
        /// <summary>
        /// Shortcut for Capabilities PourOut flag to avoid binary operators.
        /// </summary>
        public bool CanPourOut => (Capabilities & SolutionCaps.PourOut) != 0;
        /// <summary>
        /// Shortcut for Capabilities Injectable flag
        /// </summary>
        public bool Injectable => (Capabilities & SolutionCaps.Injectable) != 0;
        /// <summary>
        /// Shortcut for Capabilities Injector flag
        /// </summary>
        public bool Injector => (Capabilities & SolutionCaps.Injector) != 0;

        /// <inheritdoc />
        public override string Name => "Solution";

        /// <inheritdoc />
        public sealed override uint? NetID => ContentNetIDs.SOLUTION;

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _maxVolume, "maxVol", ReagentUnit.New(0M));
            serializer.DataField(ref ContainedSolution, "contents", new Solution());
            serializer.DataField(ref _capabilities, "caps", SolutionCaps.None);
        }

        public virtual void Init()
        {
            ContainedSolution = new Solution();
        }

        /// <inheritdoc />
        protected override void Startup()
        {
            base.Startup();
            
            RecalculateColor();
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            base.Shutdown();

            ContainedSolution.RemoveAllSolution();
            ContainedSolution = new Solution();
        }

        public void RemoveAllSolution()
        {
            ContainedSolution.RemoveAllSolution();
            OnSolutionChanged();
        }

        public bool TryRemoveReagent(string reagentId, ReagentUnit quantity)
        {
            if (!ContainsReagent(reagentId, out var currentQuantity)) return false;

            ContainedSolution.RemoveReagent(reagentId, quantity);
            OnSolutionChanged();
            return true;
        }

        /// <summary>
        /// Attempt to remove the specified quantity from this solution
        /// </summary>
        /// <param name="quantity">Quantity of this solution to remove</param>
        /// <returns>Whether or not the solution was successfully removed</returns>
        public bool TryRemoveSolution(ReagentUnit quantity)
        {
            if (CurrentVolume == 0)
                return false;

            ContainedSolution.RemoveSolution(quantity);
            OnSolutionChanged();
            return true;
        }

        public Solution SplitSolution(ReagentUnit quantity)
        {
            var solutionSplit = ContainedSolution.SplitSolution(quantity);
            OnSolutionChanged();
            return solutionSplit;
        }

        protected void RecalculateColor()
        {
            if(ContainedSolution.TotalVolume == 0)
                SubstanceColor = Color.White;

            Color mixColor = default;
            var runningTotalQuantity = ReagentUnit.New(0M);

            foreach (var reagent in ContainedSolution)
            {
                runningTotalQuantity += reagent.Quantity;

                if(!_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto))
                    continue;

                if (mixColor == default)
                    mixColor = proto.SubstanceColor;

                mixColor = BlendRGB(mixColor, proto.SubstanceColor, reagent.Quantity.Float() / runningTotalQuantity.Float());
            }
        }

         private Color BlendRGB(Color rgb1, Color rgb2, float amount)
         {
             var r     = (float)Math.Round(rgb1.R + (rgb2.R - rgb1.R) * amount, 1);
             var g     = (float)Math.Round(rgb1.G + (rgb2.G - rgb1.G) * amount, 1);
             var b     = (float)Math.Round(rgb1.B + (rgb2.B - rgb1.B) * amount, 1);
             var alpha = (float)Math.Round(rgb1.A + (rgb2.A - rgb1.A) * amount, 1);

             return new Color(r, g, b, alpha);
         }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new SolutionComponentState();
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if(curState == null)
                return;

            var compState = (SolutionComponentState)curState;

            //TODO: Make me work!
        }
        
        [Serializable, NetSerializable]
        public class SolutionComponentState : ComponentState
        {
            public SolutionComponentState() : base(ContentNetIDs.SOLUTION) { }
        }

        /// <summary>
        /// Check if the solution contains the specified reagent.
        /// </summary>
        /// <param name="reagentId">The reagent to check for.</param>
        /// <param name="quantity">Output the quantity of the reagent if it is contained, 0 if it isn't.</param>
        /// <returns>Return true if the solution contains the reagent.</returns>
        public bool ContainsReagent(string reagentId, out ReagentUnit quantity)
        {
            foreach (var reagent in ContainedSolution.Contents)
            {
                if (reagent.ReagentId == reagentId)
                {
                    quantity = reagent.Quantity;
                    return true;
                }
            }
            quantity = ReagentUnit.New(0);
            return false;
        }

        protected virtual void OnSolutionChanged()
        {
            SolutionChanged?.Invoke();
        }
    }
}
