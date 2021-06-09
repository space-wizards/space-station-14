#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;

namespace Content.Server.GameObjects.Components.Power.PowerNetComponents
{
    /// <summary>
    ///     Maintains a set of <see cref="IPowerNet"/>s that need to be updated with <see cref="IPowerNet.UpdateConsumerReceivedPower"/>.
    ///     Defers updating to reduce recalculations when a group is altered multiple times in a frame.
    /// </summary>
    public interface IPowerNetManager
    {
        /// <summary>
        ///     Queue up an <see cref="IPowerNet"/> to be updated.
        /// </summary>
        void AddDirtyPowerNet(IPowerNet powerNet);

        void Update(float frameTime);
    }

    public class PowerNetManager : IPowerNetManager
    {
        private readonly HashSet<IPowerNet> _dirtyPowerNets = new();

        public void AddDirtyPowerNet(IPowerNet powerNet)
        {
            _dirtyPowerNets.Add(powerNet);
        }

        public void Update(float frameTime)
        {
            foreach (var powerNet in _dirtyPowerNets)
            {
                powerNet.UpdateConsumerReceivedPower();
            }
            _dirtyPowerNets.Clear();
        }
    }
}
