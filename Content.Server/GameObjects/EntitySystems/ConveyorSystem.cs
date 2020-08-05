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
        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(ConveyorComponent));
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
        }
    }

    public class ConveyorGroup
    {
        private readonly HashSet<ConveyorComponent> _conveyors;
        private readonly HashSet<ConveyorSwitchComponent> _switches;

        public ConveyorGroup()
        {
            _conveyors = new HashSet<ConveyorComponent>(0);
            _switches = new HashSet<ConveyorSwitchComponent>(0);
            State = ConveyorState.Off;
        }

        public IReadOnlyCollection<ConveyorComponent> Conveyors => _conveyors;

        public IReadOnlyCollection<ConveyorSwitchComponent> Switches => _switches;

        public ConveyorState State { get; private set; }

        public void AddConveyor(ConveyorComponent conveyor)
        {
            _conveyors.Add(conveyor);
            conveyor.Sync(this);
        }

        public void RemoveConveyor(ConveyorComponent conveyor)
        {
            _conveyors.Remove(conveyor);
        }

        public void AddSwitch(ConveyorSwitchComponent conveyorSwitch)
        {
            _switches.Add(conveyorSwitch);

            if (_switches.Count == 1)
            {
                SetState(conveyorSwitch);
            }

            conveyorSwitch.Sync(this);
        }

        public void RemoveSwitch(ConveyorSwitchComponent conveyorSwitch)
        {
            _switches.Remove(conveyorSwitch);
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
    }
}
