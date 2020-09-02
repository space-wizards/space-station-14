#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Conveyor;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Conveyor
{
    [RegisterComponent]
    public class ConveyorSwitchComponent : Component, IInteractHand, IInteractUsing, IActivate
    {
        public override string Name => "ConveyorSwitch";

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

                if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                {
                    appearance.SetData(ConveyorVisuals.State, value);
                }
            }
        }

        private ConveyorGroup? _group;

        public void Sync(ConveyorGroup group)
        {
            _group = group;

            if (State == ConveyorState.Loose)
            {
                return;
            }

            State = group.State == ConveyorState.Loose
                ? ConveyorState.Off
                : group.State;
        }

        /// <summary>
        ///     Disconnects this switch from any conveyors and other switches.
        /// </summary>
        private void Disconnect()
        {
            _group?.RemoveSwitch(this);
            _group = null;
            State = ConveyorState.Off;
        }

        /// <summary>
        ///     Connects a conveyor to this switch.
        /// </summary>
        /// <param name="conveyor">The conveyor to be connected.</param>
        /// <param name="user">The user doing the connecting, if any.</param>
        public void Connect(ConveyorComponent conveyor, IEntity? user = null)
        {
            if (_group == null)
            {
                _group = new ConveyorGroup();
                _group.AddSwitch(this);
            }

            _group.AddConveyor(conveyor);
            user?.PopupMessage(Loc.GetString("Conveyor linked."));
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
            State = State switch
            {
                ConveyorState.Off => ConveyorState.Forward,
                ConveyorState.Forward => ConveyorState.Reversed,
                ConveyorState.Reversed => ConveyorState.Off,
                ConveyorState.Loose => ConveyorState.Off,
                _ => throw new ArgumentOutOfRangeException()
            };

            _group?.SetState(this);

            return true;
        }

        /// <summary>
        ///     Moves this switch to the group of another.
        /// </summary>
        /// <param name="other">The conveyor switch to synchronize with.</param>
        /// <param name="user">The user doing the syncing, if any.</param>
        private void SyncWith(ConveyorSwitchComponent other, IEntity? user = null)
        {
            other._group?.AddSwitch(this);

            if (user == null)
            {
                return;
            }

            Owner.PopupMessage(user, Loc.GetString("Switches synchronized."));
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "conveyors",
                new List<EntityUid>(),
                ids =>
                {
                    if (ids == null)
                    {
                        return;
                    }

                    foreach (var id in ids)
                    {
                        if (!Owner.EntityManager.TryGetEntity(id, out var conveyor))
                        {
                            continue;
                        }

                        if (!conveyor.TryGetComponent(out ConveyorComponent? component))
                        {
                            continue;
                        }

                        Connect(component);
                    }
                },
                () => _group?.Conveyors.Select(conveyor => conveyor.Owner.Uid).ToList());

            serializer.DataReadWriteFunction(
                "switches",
                new List<EntityUid>(),
                ids =>
                {
                    if (ids == null)
                    {
                        return;
                    }

                    foreach (var id in ids)
                    {
                        if (!Owner.EntityManager.TryGetEntity(id, out var @switch))
                        {
                            continue;
                        }

                        if (!@switch.TryGetComponent(out ConveyorSwitchComponent? component))
                        {
                            continue;
                        }

                        component.SyncWith(this);
                    }
                },
                () => _group?.Switches.Select(@switch => @switch.Owner.Uid).ToList());
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Disconnect();
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return NextState();
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (eventArgs.Using.TryGetComponent(out ConveyorComponent? conveyor))
            {
                Connect(conveyor, eventArgs.User);
                return true;
            }

            if (eventArgs.Using.TryGetComponent(out ConveyorSwitchComponent? otherSwitch))
            {
                SyncWith(otherSwitch, eventArgs.User);
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
