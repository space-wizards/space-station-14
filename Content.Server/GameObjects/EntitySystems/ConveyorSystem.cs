using System.Collections.Generic;
using Content.Server.GameObjects.Components.Conveyor;
using Content.Shared.GameObjects.Components.Conveyor;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class ConveyorSystem : EntitySystem
    {
        private uint _nextId;
        private readonly Dictionary<uint, ConveyorGroup> _groups = new Dictionary<uint, ConveyorGroup>();

        public uint NextId()
        {
            return ++_nextId;
        }

        public ConveyorGroup EnsureGroup(uint id)
        {
            if (!_groups.TryGetValue(id, out var group))
            {
                group = new ConveyorGroup(id);
                _groups[id] = group;
            }

            return group;
        }

        public void ChangeId(ConveyorComponent conveyor, uint? old, uint? current)
        {
            if (old.HasValue)
            {
                EnsureGroup(old.Value).RemoveConveyor(conveyor);
            }

            if (current.HasValue)
            {
                EnsureGroup(current.Value).AddConveyor(conveyor);
            }
        }

        public void ChangeId(ConveyorSwitchComponent conveyorSwitch, uint old, uint current)
        {
            if (old != 0)
            {
                EnsureGroup(old).RemoveSwitch(conveyorSwitch);
            }

            if (current != 0)
            {
                EnsureGroup(current).AddSwitch(conveyorSwitch);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(ConveyorComponent));
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _groups.Clear();
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                if (!entity.TryGetComponent(out ConveyorComponent conveyor))
                {
                    continue;
                }

                conveyor.Update(frameTime);
            }
        }
    }

    public class ConveyorGroup
    {
        private readonly HashSet<ConveyorComponent> _conveyors;
        private readonly HashSet<ConveyorSwitchComponent> _switches;

        public ConveyorGroup(uint id)
        {
            Id = id;
            _conveyors = new HashSet<ConveyorComponent>();
            _switches = new HashSet<ConveyorSwitchComponent>();
            State = ConveyorState.Off;
        }

        public uint Id { get; }
        public IReadOnlyCollection<ConveyorComponent> Conveyors => _conveyors;
        public IReadOnlyCollection<ConveyorSwitchComponent> Switches => _switches;
        public ConveyorState State { get; }

        public void AddConveyor(ConveyorComponent conveyor)
        {
            _conveyors.Add(conveyor);
            conveyor.SyncState(State);
        }

        public void RemoveConveyor(ConveyorComponent conveyor)
        {
            _conveyors.Remove(conveyor);
            conveyor.SyncState(ConveyorState.Off);
        }

        public void AddSwitch(ConveyorSwitchComponent conveyorSwitch)
        {
            _switches.Add(conveyorSwitch);
            conveyorSwitch.SyncState(State);
        }

        public void RemoveSwitch(ConveyorSwitchComponent conveyorSwitch)
        {
            _switches.Remove(conveyorSwitch);
            conveyorSwitch.SyncState(ConveyorState.Off);
        }

        public void SetState(ConveyorSwitchComponent conveyorSwitch)
        {
            var state = conveyorSwitch.State;

            if (state == ConveyorState.Loose)
            {
                if (_switches.Count > 0)
                {
                    return;
                }

                state = ConveyorState.Off;
            }

            foreach (var conveyor in Conveyors)
            {
                conveyor.SyncState(state);
            }

            foreach (var connectedSwitch in _switches)
            {
                connectedSwitch.SyncState(state);
            }
        }
    }
}
