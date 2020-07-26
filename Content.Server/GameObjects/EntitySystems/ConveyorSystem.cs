using System;
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
        private readonly Dictionary<uint, ConveyorGroup> _groups = new Dictionary<uint, ConveyorGroup>();

        public uint NextId()
        {
            uint id = 1;

            while (_groups.ContainsKey(id))
            {
                id++;
            }

            return id;
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

        public void ChangeId(ConveyorSwitchComponent conveyorSwitch, uint old, uint @new)
        {
            if (old != 0)
            {
                EnsureGroup(old).RemoveSwitch(conveyorSwitch);
            }

            if (@new != 0)
            {
                EnsureGroup(@new).AddSwitch(conveyorSwitch);
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

                conveyor.Update();
            }

            foreach (var (id, group) in _groups)
            {
                if (group.IsEmpty())
                {
                    group.Dispose();
                    _groups.Remove(id);
                }
            }
        }
    }

    public class ConveyorGroup : IDisposable
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

        public ConveyorState State { get; set; }

        public void AddConveyor(ConveyorComponent conveyor)
        {
            _conveyors.Add(conveyor);
            conveyor.Sync(this);
        }

        public void RemoveConveyor(ConveyorComponent conveyor)
        {
            _conveyors.Remove(conveyor);
            conveyor.Disconnect();
        }

        public void AddSwitch(ConveyorSwitchComponent conveyorSwitch)
        {
            _switches.Add(conveyorSwitch);
            conveyorSwitch.Sync(this);
        }

        public void RemoveSwitch(ConveyorSwitchComponent conveyorSwitch)
        {
            _switches.Remove(conveyorSwitch);
            conveyorSwitch.Disconnect();
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

            State = state;

            foreach (var conveyor in Conveyors)
            {
                conveyor.Sync(this);
            }

            foreach (var connectedSwitch in _switches)
            {
                connectedSwitch.Sync(this);
            }
        }

        public bool IsEmpty()
        {
            return _conveyors.Count == 0 && _switches.Count == 0;
        }

        public void Dispose()
        {
            _conveyors.Clear();
            _switches.Clear();
        }
    }
}
