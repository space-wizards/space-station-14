using System.Collections.Generic;
using Content.Server.GameObjects.Components.Disposal;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class DisposalSystem : EntitySystem
    {
        private uint _lastUid;
        private readonly Dictionary<uint, DisposalNet> _disposalNets = new Dictionary<uint, DisposalNet>();

        public DisposalSystem()
        {
            EntityQuery = new TypeEntityQuery(typeof(IDisposalTubeComponent));
        }

        public uint NewUid()
        {
            return ++_lastUid;
        }

        public bool Add(DisposalNet net)
        {
            return _disposalNets.TryAdd(net.Uid, net);
        }

        public bool Remove(DisposalNet net)
        {
            return _disposalNets.Remove(net.Uid);
        }

        public override void Update(float frameTime)
        {
            for (uint i = 0; i < _disposalNets.Count; i++)
            {
                if (!_disposalNets.TryGetValue(i, out var net))
                {
                    continue;
                }

                if (net.Dirty)
                {
                    net.Reconnect();
                    i--;
                }
            }

            foreach (var disposalNet in _disposalNets.Values)
            {
                disposalNet.Update(frameTime);
            }
        }
    }
}
