using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Shared.GameObjects.Components.Conveyor;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Conveyor
{
    [RegisterComponent]
    public class ConveyorSwitchComponent : Component, IInteractHand, IExamine, IInteractUsing, IActivate
    {
        public override string Name => "ConveyorSwitch";

        [ViewVariables]
        private uint _id;

        private ConveyorState _state;

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

        private ConveyorGroup Group => EntitySystem.Get<ConveyorSystem>().EnsureGroup(_id);

        public void Sync(ConveyorGroup group)
        {
            _id = group.Id;

            if (State == ConveyorState.Loose)
            {
                return;
            }

            State = group.State == ConveyorState.Loose
                ? ConveyorState.Off
                : group.State;
        }

        public void Disconnect()
        {
            _id = 0;
            State = ConveyorState.Off;
        }

        public void ChangeId(uint id)
        {
            EntitySystem.Get<ConveyorSystem>().ChangeId(this, _id, id);
        }

        public void Connect(IEntity user, ConveyorComponent conveyor)
        {
            Group.AddConveyor(conveyor);

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

        private void SyncWith(IEntity user, ConveyorSwitchComponent other)
        {
            _id = other._id;
            Owner.PopupMessage(user, Loc.GetString("Switch changed to id {0}.", _id));
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction<uint>(
                "id",
                0,
                id => _id = id == 0 ? EntitySystem.Get<ConveyorSystem>().NextId() : id,
                () => _id);
        }

        public override void OnRemove()
        {
            base.OnRemove();

            Group?.RemoveSwitch(this);
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

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            NextState();
        }
    }
}
