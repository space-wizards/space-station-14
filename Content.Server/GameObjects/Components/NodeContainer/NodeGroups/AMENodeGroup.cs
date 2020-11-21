using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Explosions;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.Components.Power.AME;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    /// <summary>
    /// Node group class for handling the Antimatter Engine's console and parts.
    /// </summary>
    [NodeGroup(NodeGroupID.AMEngine)]
    public class AMENodeGroup : BaseNodeGroup
    {
        /// <summary>
        /// The AME controller which is currently in control of this node group.
        /// This could be tracked a few different ways, but this is most convenient,
        /// since any part connected to the node group can easily find the master.
        /// </summary>
        [ViewVariables]
        private AMEControllerComponent _masterController;

        public AMEControllerComponent MasterController => _masterController;

        private readonly List<AMEShieldComponent> _cores = new List<AMEShieldComponent>();

        public int CoreCount => _cores.Count;

        protected override void OnAddNode(Node node)
        {
            base.OnAddNode(node);
            if (_masterController == null)
            {
                node.Owner.TryGetComponent<AMEControllerComponent>(out var controller);
                _masterController = controller;
            }
        }

        protected override void OnRemoveNode(Node node)
        {
            base.OnRemoveNode(node);
            RefreshAMENodes(_masterController);
            if (_masterController != null && _masterController?.Owner == node.Owner) { _masterController = null; }
        }

        public void RefreshAMENodes(AMEControllerComponent controller)
        {
            if(_masterController == null && controller != null)
            {
                _masterController = controller;
            }

            if (_cores != null) {
                foreach (AMEShieldComponent core in _cores)
                {
                    core.UnsetCore();
                }
                _cores.Clear();
            }

            //Check each shield node to see if it meets core criteria
            foreach (Node node in Nodes)
            {
                if (!node.Owner.TryGetComponent<AMEShieldComponent>(out var shield)) { continue; }
                var nodeNeighbors = node.Owner
                    .GetComponent<SnapGridComponent>()
                    .GetCellsInSquareArea()
                    .Select(sgc => sgc.Owner)
                    .Where(entity => entity != node.Owner)
                    .Select(entity => entity.TryGetComponent<AMEShieldComponent>(out var adjshield) ? adjshield : null)
                    .Where(adjshield => adjshield != null);

                if (nodeNeighbors.Count() >= 8) { _cores.Add(shield); }
            }

            if (_cores == null) { return; }

            foreach (AMEShieldComponent core in _cores)
            {
                core.SetCore();
            }
        }

        public void UpdateCoreVisuals(int injectionAmount, bool injecting)
        {

            var injectionStrength = CoreCount > 0 ? injectionAmount / CoreCount : 0;

            foreach (AMEShieldComponent core in _cores)
            {
                core.UpdateCoreVisuals(injectionStrength, injecting);
            }
        }

        public int InjectFuel(int injectionAmount)
        {
            if(injectionAmount > 0 && CoreCount > 0)
            {
                var instability = 2 * (injectionAmount / CoreCount);
                foreach(AMEShieldComponent core in _cores)
                {
                    core.CoreIntegrity -= instability;
                }
                return CoreCount * injectionAmount * 15000; //2 core engine injecting 2 fuel per core = 60kW(?)
            }
            return 0;
        }

        public int GetTotalStability()
        {
            if(CoreCount < 1) { return 100; }
            var stability = 0;

            foreach(AMEShieldComponent core in _cores)
            {
                stability += core.CoreIntegrity;
            }

            stability = stability / CoreCount;

            return stability;
        }

        public void ExplodeCores()
        {
            if(_cores.Count < 1 || MasterController == null) { return; }

            var intensity = 0;

            /*
             * todo: add an exact to the shielding and make this find the core closest to the controller
             * so they chain explode, after helpers have been added to make it not cancer
            */
            var epicenter = _cores.First();

            foreach (AMEShieldComponent core in _cores)
            {
                intensity += MasterController.InjectionAmount;
            }

            intensity = Math.Min(intensity, 8);

            epicenter.Owner.SpawnExplosion(intensity / 2, intensity, intensity * 2, intensity * 3);
        }
    }
}
