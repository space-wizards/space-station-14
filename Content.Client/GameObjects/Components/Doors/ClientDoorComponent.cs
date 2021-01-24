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
    public class ClientDoorComponent : SharedDoorComponent
    {
        private bool _stateChangeHasProgressed = false;
        private TimeSpan _timeOffset;

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

        public override void OnUpdate(float frameTime)
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
}
