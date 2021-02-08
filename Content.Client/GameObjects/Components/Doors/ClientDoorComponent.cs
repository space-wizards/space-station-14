#nullable enable
using Content.Shared.GameObjects.Components.Doors;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Client.GameObjects.Components.Doors
{
    /// <summary>
    /// Bare-bones client-side door component; used to stop door-based mispredicts.
    /// </summary>
    [UsedImplicitly]
    [RegisterComponent]
    [ComponentReference(typeof(SharedDoorComponent))]
    public class ClientDoorComponent : SharedDoorComponent
    {
        private bool _stateChangeHasProgressed = false;
        private TimeSpan _timeOffset;

        public override DoorState State
        {
            protected set
            {
                if (State == value)
                {
                    return;
                }

                base.State = value;

                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new DoorStateMessage(this, State));
            }
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not DoorComponentState doorCompState)
            {
                return;
            }

            CurrentlyCrushing = doorCompState.CurrentlyCrushing;
            StateChangeStartTime = doorCompState.StartTime;
            State = doorCompState.DoorState;

            if (StateChangeStartTime == null)
            {
                return;
            }

            _timeOffset = State switch
            {
                DoorState.Opening => OpenTimeOne,
                DoorState.Closing => CloseTimeOne,
                _ => throw new ArgumentOutOfRangeException(),
            };

            if (doorCompState.CurTime >= StateChangeStartTime + _timeOffset)
            {
                _stateChangeHasProgressed = true;
                return;
            }

            _stateChangeHasProgressed = false;
        }

        public void OnUpdate()
        {
            if (!_stateChangeHasProgressed)
            {
                if (GameTiming.CurTime < StateChangeStartTime + _timeOffset) return;

                if (State == DoorState.Opening)
                {
                    OnPartialOpen();
                }
                else
                {
                    OnPartialClose();
                }

                _stateChangeHasProgressed = true;
                Dirty();
            }
        }
    }

    public sealed class DoorStateMessage : EntitySystemMessage
    {
        public ClientDoorComponent Component { get; }
        public SharedDoorComponent.DoorState State { get; }

        public DoorStateMessage(ClientDoorComponent component, SharedDoorComponent.DoorState state)
        {
            Component = component;
            State = state;
        }
    }
}
