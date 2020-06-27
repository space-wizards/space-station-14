using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    public class DisposalNet
    {
        /// <summary>
        ///     Set of disposables currently inside this DisposalNet
        /// </summary>
        private readonly HashSet<InDisposalsComponent> _contents;

        /// <summary>
        ///     Set of tubes that make up the DisposalNet
        /// </summary>
        private readonly HashSet<IDisposalTubeComponent> _tubeList;

        public DisposalNet()
        {
            var disposalSystem = EntitySystem.Get<DisposalSystem>();
            disposalSystem.Add(this);
            Uid = disposalSystem.NewUid();
            _tubeList = new HashSet<IDisposalTubeComponent>();
            _contents = new HashSet<InDisposalsComponent>();
            TravelTime = 0.1f;
        }

        /// <summary>
        /// Unique identifier per DisposalNet
        /// </summary>
        [ViewVariables]
        public uint Uid { get; }

        /// <summary>
        /// If true, this DisposalNet will be regenerated from its tubes
        /// during the next update cycle.
        /// </summary>
        [ViewVariables]
        public bool Dirty { get; private set; }

        /// <summary>
        /// The time that it takes for an entity within this DisposalNet to
        /// travel between two of its tubes
        /// </summary>
        [ViewVariables]
        public float TravelTime { get; set; }

        public void Add(IDisposalTubeComponent tube)
        {
            _tubeList.Add(tube);

            foreach (var entity in tube.ContainedEntities)
            {
                if (!entity.TryGetComponent(out InDisposalsComponent disposable))
                {
                    continue;
                }

                Insert(disposable);
            }
        }

        public void Remove(IDisposalTubeComponent tube)
        {
            _tubeList.Remove(tube);

            foreach (var entity in tube.ContainedEntities)
            {
                if (!entity.TryGetComponent(out InDisposalsComponent disposable))
                {
                    continue;
                }

                Remove(disposable);
            }

            Dirty = true;
        }

        public void Insert(InDisposalsComponent inDisposals)
        {
            _contents.Add(inDisposals);
        }

        public void Remove(InDisposalsComponent inDisposals)
        {
            _contents.Remove(inDisposals);
        }

        private void Dispose()
        {
            foreach (var tube in _tubeList.ToHashSet())
            {
                tube.DisconnectFromNet();
                Remove(tube);
            }

            foreach (var disposable in _contents.ToHashSet())
            {
                disposable.ExitDisposals();
                Remove(disposable);
            }

            _tubeList.Clear();
            _contents.Clear();
            EntitySystem.Get<DisposalSystem>().Remove(this);
        }

        public void MergeNets(DisposalNet net)
        {
            foreach (var tube in net._tubeList)
            {
                tube.ConnectToNet(this);
            }

            net.Dispose();
        }

        public void Reconnect()
        {
            foreach (var tube in _tubeList)
            {
                tube.Reconnecting = true;
            }

            foreach (var tube in _tubeList)
            {
                if (tube.Reconnecting)
                {
                    tube.SpreadDisposalNet();
                }
            }

            Dispose();
        }

        public void Update(float frameTime)
        {
            foreach (var disposable in _contents.ToHashSet())
            {
                disposable.Update(frameTime);
            }
        }
    }
}
