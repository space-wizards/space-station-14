#nullable enable
using Content.Shared.GameObjects.Components.Doors;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using System;

namespace Content.Client.GameObjects.Components.Doors
{
    [UsedImplicitly]
    [RegisterComponent]
    public class ClientDoorComponent : SharedDoorComponent
    {
        private DoorState State
        {
            get => _state;
            set
            {
                if (_state == value)
                {
                    return;
                }

                _state = value;
                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new DoorStateMessage(this, State));
            }
        }

        private bool _stateChangeHasProgressed = false;

        private TimeSpan _timeOne;
        private TimeSpan _timeTwo;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not DoorComponentState doorCompState)
            {
                return;
            }

            CurrentlyCrushing = doorCompState.CurrentlyCrushing;
            State = doorCompState.DoorState;
            StateChangeStartTime = doorCompState.StartTime;

            if(StateChangeStartTime == null)
            {
                return;
            }

            switch (State)
            {
                case DoorState.Opening:
                    _timeOne = OpenTimeOne;
                    _timeTwo = OpenTimeTwo;
                    break;
                case DoorState.Closing:
                    _timeOne = CloseTimeOne;
                    _timeTwo = CloseTimeTwo;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (doorCompState.CurTime >= StateChangeStartTime + _timeOne)
            {
                _stateChangeHasProgressed = true;
                return;
            }

            _stateChangeHasProgressed = false;
        }

        public override void OnUpdate(float frameTime)
        {
            if (_stateChangeHasProgressed)
            {
                if (GameTiming.CurTime < StateChangeStartTime + _timeOne + _timeTwo) return;

                if (State == DoorState.Opening)
                {
                    State = DoorState.Open;
                }
                else
                {
                    OnFullClose();
                    State = DoorState.Closed;
                }

                StateChangeStartTime = null;
            }

            else
            {
                if (GameTiming.CurTime < StateChangeStartTime + _timeOne) return;

                if (State == DoorState.Opening)
                {
                    OnPartialOpen();
                }
                else
                {
                    OnPartialClose();
                }

                _stateChangeHasProgressed = true;
            }

            Dirty();
        }
    }
}
