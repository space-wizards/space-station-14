using System;
using System.Collections.Generic;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    public class SolutionComponent : Component
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        [ViewVariables]
        protected Solution _containedSolution = new Solution();
        protected int _maxVolume;
        private SolutionCaps _capabilities;

        /// <summary>
        /// Triggered when the solution contents change.
        /// </summary>
        public event Action SolutionChanged;

        /// <summary>
        ///     The maximum volume of the container.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxVolume
        {
            get => _maxVolume;
            set => _maxVolume = value; // Note that the contents won't spill out if the capacity is reduced.
        }

        /// <summary>
        ///     The total volume of all the of the reagents in the container.
        /// </summary>
        [ViewVariables]
        public int CurrentVolume => _containedSolution.TotalVolume;

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

        public IReadOnlyList<Solution.ReagentQuantity> ReagentList => _containedSolution.Contents;

        /// <inheritdoc />
        public override string Name => "Solution";

        /// <inheritdoc />
        public sealed override uint? NetID => ContentNetIDs.SOLUTION;

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _maxVolume, "maxVol", 0);
            serializer.DataField(ref _containedSolution, "contents", _containedSolution);
            serializer.DataField(ref _capabilities, "caps", SolutionCaps.None);
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

            _containedSolution.RemoveAllSolution();
            _containedSolution = new Solution();
        }

        public void RemoveAllSolution()
        {
            _containedSolution.RemoveAllSolution();
            OnSolutionChanged();
        }

        public bool TryRemoveReagent(string reagentId, int quantity)
        {
            if (!ContainsReagent(reagentId, out var currentQuantity)) return false;

            _containedSolution.RemoveReagent(reagentId, quantity);
            OnSolutionChanged();
            return true;
        }

        public bool TryRemoveSolution(int quantity)
        {
            if (CurrentVolume == 0) return false;

            _containedSolution.RemoveSolution(quantity);
            OnSolutionChanged();
            return true;
        }

        public Solution SplitSolution(int quantity)
        {
            var solutionSplit = _containedSolution.SplitSolution(quantity);
            OnSolutionChanged();
            return solutionSplit;
        }

        protected void RecalculateColor()
        {
            if(_containedSolution.TotalVolume == 0)
                SubstanceColor = Color.White;

            Color mixColor = default;
            float runningTotalQuantity = 0;

            foreach (var reagent in _containedSolution)
            {
                runningTotalQuantity += reagent.Quantity;

                if(!_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto))
                    continue;

                if (mixColor == default)
                    mixColor = proto.SubstanceColor;

                mixColor = BlendRGB(mixColor, proto.SubstanceColor, reagent.Quantity / runningTotalQuantity);
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
        public bool ContainsReagent(string reagentId, out int quantity)
        {
            foreach (var reagent in _containedSolution.Contents)
            {
                if (reagent.ReagentId == reagentId)
                {
                    quantity = reagent.Quantity;
                    return true;
                }
            }
            quantity = 0;
            return false;
        }

        protected virtual void OnSolutionChanged()
        {
            SolutionChanged?.Invoke();
        }
    }
}
