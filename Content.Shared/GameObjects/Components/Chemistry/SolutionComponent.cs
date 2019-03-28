using System;
using System.Collections.Generic;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    public class SolutionComponent : Component
    {
        private Solution _containedSolution;
        private int _maxVolume;
        private SolutionCaps _capabilities;

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

        public SolutionCaps Capabilities
        {
            get => _capabilities;
            set => _capabilities = value;
        }

        /// <inheritdoc />
        public override string Name => "Solution";

        /// <inheritdoc />
        public sealed override uint? NetID => ContentNetIDs.SOLUTION;

        /// <inheritdoc />
        public sealed override Type StateType => typeof(SolutionComponentState);
        
        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _maxVolume, "maxVol", 0);
            serializer.DataField(ref _containedSolution, "contents", new Solution());
            serializer.DataField(ref _capabilities, "caps", SolutionCaps.None);
        }

        public override void Startup()
        {
            base.Startup();
            
            //TODO: Calculate color
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _containedSolution.RemoveAllSolution();
            _containedSolution = new Solution();
        }

        public bool TryAddReagent(string reagentId, int quantity, out int acceptedQuantity)
        {
            throw new NotImplementedException();
        }

        public bool TryAddSolution(Solution solution, int quantity)
        {
            throw new NotImplementedException();
        }
        
        public List<(string reagentId, int quantity)> TryRemoveSolution(int quantity)
        {
            throw new NotImplementedException();
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
    }
}
