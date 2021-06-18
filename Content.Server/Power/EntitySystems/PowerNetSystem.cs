#nullable enable
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    public class PowerNetSystem : EntitySystem
    {
        private readonly HashSet<IPowerNet> _dirtyPowerNets = new();

        public void AddDirtyPowerNet(IPowerNet powerNet)
        {
            _dirtyPowerNets.Add(powerNet);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var powerNet in _dirtyPowerNets)
            {
                powerNet.UpdateConsumerReceivedPower();
            }

            _dirtyPowerNets.Clear();
        }


    }
}
