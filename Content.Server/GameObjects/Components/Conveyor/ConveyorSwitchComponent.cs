using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.Components.Conveyor;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Conveyor
{
    [RegisterComponent]
    public class ConveyorSwitchComponent : Component, IInteractHand, IExamine, IInteractUsing, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly IServerEntityManager _entityManager;
#pragma warning restore 649

        public override string Name => "ConveyorSwitch";

        private uint _id;

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
                var oldValue = _state;
                _state = value;

                if (Owner.TryGetComponent(out AppearanceComponent appearance))
                {
                    appearance.SetData(ConveyorSwitchVisuals.State, value);
                }

                if (oldValue != value)
                {
                    foreach (var conveyor in _connections)
                    {
                        conveyor.ChangeState(State);
                    }
                }

            }
        }

        [ViewVariables]
        private bool Anchored => !Owner.TryGetComponent(out PhysicsComponent physics) ||
                                 physics.Anchored;

        /// <summary>
        ///     Finds all conveyors connected to this switch
        /// </summary>
        /// <returns>An enumerable of the conveyors found</returns>
        private IEnumerable<ConveyorComponent> FindConveyors()
        {
            var conveyors = _entityManager.ComponentManager.GetAllComponents<ConveyorComponent>();

            foreach (var conveyor in conveyors)
            {
                if (conveyor.Id != _id)
                {
                    continue;
                }

                yield return conveyor;
            }
        }

        public void Connect(IEntity user, ConveyorComponent conveyor)
        {
            conveyor.Id = _id;
            _connections.Add(conveyor);

            user.PopupMessage(user, Loc.GetString("Conveyor linked with id {0}.", _id));
        }

        /// <summary>
        ///     Cycles this conveyor switch to its next valid state
        /// </summary>
        /// <returns>
        ///     true if the switch can be operated and the state could be cycled,
        ///     false otherwise
        /// </returns>
        private bool NextState()
        {
            if (!Anchored)
            {
                State = 0;
                return false;
            }

            var last = Enum.GetValues(typeof(ConveyorState)).Cast<ConveyorState>().Max();

            State = State == last ? 0 : _state + 1;

            return true;
        }

        private bool ToolUsed(IEntity user, ToolComponent tool)
        {
            if (!Owner.HasComponent<ItemComponent>() &&
                tool.UseTool(user, Owner, ToolQuality.Prying))
            {
                Owner.AddComponent<ItemComponent>();
                NextState();
                return true;
            }

            return false;
        }

        private void AnchoredChanged()
        {
            if (!Anchored)
            {
                NextState();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponent<ItemComponent>();
            Owner.EnsureComponent<AnchorableComponent>();
            _id = EntitySystem.Get<ConveyorSystem>().NextId();

            var physics = Owner.EnsureComponent<PhysicsComponent>();
            physics.AnchoredChanged += AnchoredChanged;
        }

        protected override void Startup()
        {
            base.Startup();

            _connections = FindConveyors().ToHashSet();
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return NextState();
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("It has an id of {0}.", _id));
        }

        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (eventArgs.Using.TryGetComponent(out ToolComponent tool))
            {
                return ToolUsed(eventArgs.User, tool);
            }

            if (!eventArgs.Using.TryGetComponent(out ConveyorComponent conveyor))
            {
                return false;
            }

            Connect(eventArgs.User, conveyor);
            return true;
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out HandsComponent hands))
            {
                return;
            }

            if (!Owner.HasComponent<ItemComponent>() ||
                !eventArgs.CanReach ||
                eventArgs.Target != null)
            {
                return;
            }

            if (!hands.Drop(Owner))
            {
                return;
            }

            Owner.Transform.GridPosition = eventArgs.ClickLocation;
        }
    }
}
