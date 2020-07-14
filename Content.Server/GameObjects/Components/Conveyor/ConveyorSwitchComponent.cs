using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.Components.Conveyor;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Conveyor
{
    [RegisterComponent]
    public class ConveyorSwitchComponent : Component, IInteractHand
    {
#pragma warning disable 649
        [Dependency] private readonly IServerEntityManager _entityManager;
#pragma warning restore 649

        public override string Name => "ConveyorSwitch";

        /// <summary>
        ///     The set of conveyors connected to this lever
        /// </summary>
        [ViewVariables]
        private HashSet<ConveyorComponent> _connections;

        private ConveyorState _state;

        /// <summary>
        ///     The current state of this switch
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private ConveyorState State
        {
            get => _state;
            set
            {
                _state = value;

                if (!Owner.TryGetComponent(out AppearanceComponent appearance))
                {
                    return;
                }

                appearance.SetData(ConveyorSwitchVisuals.State, value);
            }
        }

        /// <summary>
        ///     Finds all conveyors connected to this switch
        /// </summary>
        /// <returns>An enumerable of the conveyors found</returns>
        private IEnumerable<ConveyorComponent> FindConveyors()
        {
            var entities = _entityManager.GetEntitiesInRange(Owner, 1);

            foreach (var entity in entities)
            {
                if (!entity.TryGetComponent(out ConveyorComponent conveyor))
                {
                    continue;
                }

                yield return conveyor;
            }
        }

        /// <summary>
        ///     Cycles this conveyor switch to its next valid state
        /// </summary>
        /// <returns>true if the state was changed, false otherwise</returns>
        private bool NextState()
        {
            var last = Enum.GetValues(typeof(ConveyorState)).Cast<ConveyorState>().Max();

            State = State == last ? 0 : _state + 1;

            foreach (var conveyor in _connections)
            {
                conveyor.ChangeState(State);
            }

            return true;
        }

        public override void Initialize()
        {
            base.Initialize();

            _connections = new HashSet<ConveyorComponent>();
        }

        protected override void Startup()
        {
            base.Startup();

            foreach (var conveyor in FindConveyors())
            {
                _connections.Add(conveyor);
            }
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return NextState();
        }
    }
}
