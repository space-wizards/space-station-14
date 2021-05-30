#nullable enable
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System.Collections.Generic;
using Content.Shared.GameTicking;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ApcNetSystem : EntitySystem, IResettingEntitySystem
    {
        [Dependency] private readonly IPauseManager _pauseManager = default!;

        private HashSet<IApcNet> _apcNets = new();

        public override void Update(float frameTime)
        {
            foreach (var apcNet in _apcNets)
            {
                var gridId = apcNet.GridId;
                if (gridId != null && !_pauseManager.IsGridPaused(gridId.Value))
                    apcNet.Update(frameTime);
            }
        }

        public void AddApcNet(ApcNetNodeGroup apcNet)
        {
            _apcNets.Add(apcNet);
        }

        public void RemoveApcNet(ApcNetNodeGroup apcNet)
        {
            _apcNets.Remove(apcNet);
        }

        public void Reset()
        {
            // NodeGroupSystem does not remake ApcNets affected during restarting until a frame later,
            // when their grid is invalid. So, we are clearing them on round restart.
            _apcNets.Clear();
        }
    }
}
