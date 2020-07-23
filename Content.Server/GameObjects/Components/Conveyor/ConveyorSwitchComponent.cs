using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Shared.GameObjects.Components.Conveyor;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Conveyor
{
    [RegisterComponent]
    public class ConveyorSwitchComponent : Component, IInteractHand, IExamine, IInteractUsing, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly IServerEntityManager _entityManager;
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

        public override string Name => "ConveyorSwitch";

        private uint _id;
        private ConveyorState _state;

        [ViewVariables]
        private uint Id
        {
            get => _id;
            set
            {
                var old = _id;
                _id = value;

                EntitySystem.Get<ConveyorSystem>().ChangeId(this, old, _id);
            }
        }

        /// <summary>
        ///     The current state of this switch
        /// </summary>
        [ViewVariables]
        public ConveyorState State
        {
            get => _state;
            private set
            {
                _state = value;

                if (Owner.TryGetComponent(out AppearanceComponent appearance))
                {
                    appearance.SetData(ConveyorVisuals.State, value);
                }
            }
        }

        private ConveyorGroup Group => EntitySystem.Get<ConveyorSystem>().EnsureGroup(Id);

        /// <summary>
        ///     Syncs this switch with the state of another
        /// </summary>
        /// <param name="state">The state to sync with</param>
        public void SyncState(ConveyorState state)
        {
            if (State == ConveyorState.Loose)
            {
                return;
            }

            State = state == ConveyorState.Loose
                ? ConveyorState.Off
                : state;
        }

        public void Connect(IEntity user, ConveyorComponent conveyor)
        {
            Group.AddConveyor(conveyor);

            user.PopupMessage(user, Loc.GetString("Conveyor linked with id {0}.", Id));
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
            if (Owner.HasComponent<ItemComponent>())
            {
                State = ConveyorState.Loose;
                Group.SetState(this);

                return false;
            }

            State = State switch
            {
                ConveyorState.Off => ConveyorState.Forward,
                ConveyorState.Forward => ConveyorState.Reversed,
                ConveyorState.Reversed => ConveyorState.Off,
                ConveyorState.Loose => ConveyorState.Off,
                _ => throw new ArgumentOutOfRangeException()
            };

            Group.SetState(this);

            return true;
        }

        private bool ToolUsed(IEntity user, ToolComponent tool)
        {
            if (!Owner.HasComponent<ItemComponent>() &&
                tool.UseTool(user, Owner, ToolQuality.Prying))
            {
                return Itemize();
            }

            return false;
        }

        private void SyncWith(IEntity user, ConveyorSwitchComponent other)
        {
            Id = other.Id;
            Owner.PopupMessage(user, Loc.GetString("Switch changed to id {0}.", Id));
        }

        private bool Itemize()
        {
            if (Owner.HasComponent<ItemComponent>())
            {
                return false;
            }

            Owner.AddComponent<ItemComponent>();
            NextState();
            Owner.Transform.WorldPosition += (_random.NextFloat() * 0.4f - 0.2f, _random.NextFloat() * 0.4f - 0.2f);

            return true;
        }

        private void DeItemize(GridCoordinates coordinates)
        {
            if (!Owner.HasComponent<ItemComponent>())
            {
                return;
            }

            Owner.Transform.GridPosition = coordinates;
            Owner.RemoveComponent<ItemComponent>();
            NextState();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction<uint?>(
                "id",
                null,
                id => Id = id ?? EntitySystem.Get<ConveyorSystem>().NextId(),
                () => Id);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return NextState();
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("It has an id of {0}.", Id));
        }

        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (eventArgs.Using.TryGetComponent(out ToolComponent tool))
            {
                return ToolUsed(eventArgs.User, tool);
            }

            if (eventArgs.Using.TryGetComponent(out ConveyorComponent conveyor))
            {
                Connect(eventArgs.User, conveyor);
                return true;
            }

            if (eventArgs.Using.TryGetComponent(out ConveyorSwitchComponent otherSwitch))
            {
                SyncWith(eventArgs.User, otherSwitch);
                return true;
            }

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

            DeItemize(eventArgs.ClickLocation);
        }
    }
}
