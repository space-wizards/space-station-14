#nullable enable
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using System.Collections.Generic;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ApcNetSystem : EntitySystem
    {
        [Dependency] private readonly IPauseManager _pauseManager = default!;

        private HashSet<IApcNet> _apcNets = new();

        public override void Update(float frameTime)
        {
            foreach (var apcNet in _apcNets)
            {
                var gridId = apcNet.GridId;
                if (gridId != null && !_pauseManager.IsGridPaused((GridId) gridId))
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
    }
}
